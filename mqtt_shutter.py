#!/usr/bin/env python3

from HBridge import HBridge
import paho.mqtt.client as mqtt
import time
from enum import Enum, unique

class Shutter():

    @unique
    class ShutterStatus(Enum):
        CLOSED = 0
        OPEN = 1
        STOPPED = 2

    def __init__(self, name, forward_in, forward_out, reverse_in, reverse_out, mqtt_client, half_duration):
        self.hbridge = HBridge(forward_in, forward_out, reverse_in, reverse_out)
        self.client = mqtt_client
        self.name = name
        self.half_duration = half_duration
        self.status_topic = 'home/shutter/{}/status'.format(self.name)
        self.request_topic = 'home/shutter/{}/request'.format(self.name)
        self.client.subscribe(self.request_topic)

    def close(self):
        self.hbridge.forward()
        self.client.publish(
            self.status_topic,
            'CLOSED', 2, True
        )
        self.status = ShutterStatus.CLOSED

    def open(self):
        self.hbridge.reverse()
        self.client.publish(
            self.status_topic,
            'OPEN', 2, True
        )
        self.status = ShutterStatus.OPEN

    def stop(self):
        self.hbridge.off()
        self.client.publish(
            self.status_topic,
            'STOPPED', 2, True
        )
        self.status = ShutterStatus.STOPPED

    def half(self):
        if (self.status is not ShutterStatus.OPEN):
            self.open()
            time.sleep(2 * self.half_duration)
        self.close()
        time.sleep(self.half_duration)
        self.stop()


def on_connect(client, userdata, flags, rc):
    if (rc == 0):
        client.publish('home/shutter/status', 'ONLINE', 2, True)

def on_disconnect(client, userdata, rc):
    client.publish('home/shutter/status', 'OFFLINE', 2, True)

def on_message(client, userdata, message):
    if (not message.payload): return
    client.publish(message.topic, None, retain = True, qos = 2)

    shutter_name = message.topic.split('/')[2]
    action = str(message.payload.decode('utf-8'))



class Mqtt_Shutter():

    def __init__(self):
        self.client = mqtt.Client(
            'rpi-shutter-controller',
            userdata={
                'username': 'rpi2',
                'password': 'dbx-102'
            },
            protocol = mqtt.MQTTv311
        )

        self.shutters = {
            'living' : Shutter('living', (0, 1), (0, 2), (0, 3), (0, 4), client, 14),
            'stairs' : Shutter('stairs', (1, 1), (1, 2), (1, 3), (1, 4), client, 12),
            'master' : Shutter('master', (3, 1), (3, 2), (3, 3), (3, 4), client, 12),
            'sarah'  : Shutter('sarah', (0, 5), (1, 5), (3, 5), (0, 6), client, 12)
            }

        def on_connect(self, client, userdata, flags, rc):
            if (rc == 0):
                client.publish('home/shutter/status', 'ONLINE', 2, True)

        def on_disconnect(self, client, userdata, rc):
            client.publish('home/shutter/status', 'OFFLINE', 2, True)

        def on_message(self, client, userdata, message):
            if (not message.payload): return
            client.publish(message.topic, None, retain = True, qos = 2)

            shutter_name = message.topic.split('/')[2]
            action = str(message.payload.decode('utf-8'))
            if (action == "OPEN"):
                self.shutters[shutter_name].open()
            elif (action == "CLOSE"):
                self.shutters[shutter_name].close()
            elif (action == "STOP"):
                self.shutters[shutter_name].stop()
            elif (action == "HALF"):
                self.shutters[shutter_name].half()

        def mainloop(self):
            self.client.loop_forever()