#!/usr/bin/python3

from HBridge import HBridge
import paho.mqtt.client as mqtt
import time
from enum import Enum, unique
import logging
from systemd.journal import JournalHandler



@unique
class ShutterStatus(Enum):
    CLOSED = 0
    OPEN = 1
    STOPPED = 2
    UNKNOWN = 3


class Shutter():

    def __init__(self, name, forward_in, forward_out, reverse_in, reverse_out, mqtt_client, half_duration):
        self.hbridge = HBridge(forward_in, forward_out, reverse_in, reverse_out)
        self.client = mqtt_client
        self.name = name
        self.half_duration = half_duration
        self.status_topic = 'home/shutter/{}/status'.format(self.name)
        self.request_topic = 'home/shutter/{}/request'.format(self.name)
        self.status = ShutterStatus.UNKNOWN

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


    def __init__(self, logger=None):
        self.client = mqtt.Client(
            'rpi-shutter-controller',
            userdata={
                'username': 'rpi2',
                'password': 'dbx-102'
            },
            protocol = mqtt.MQTTv311
        )
        if not logger:
            logging.basicConfig(level=logging.WARNING)
            self.logger = logging.getLogger(name='shutter')
        else:
            self.logger = logger
        self.client.enable_logger(logger)
        self.client.will_set('home/shutter/status', 'CONNECTION LOST', 1, True)
        self.client.on_message=self.on_message
        self.client.on_connect=self.on_connect
        self.client.on_disconnect=self.on_disconnect
        self.shutters = {
            'living' : Shutter('living', (0, 1), (0, 2), (0, 3), (0, 4), self.client, 11),
            'stairs' : Shutter('stairs', (1, 1), (1, 2), (1, 3), (1, 4), self.client, 9),
            'master' : Shutter('master', (3, 1), (3, 2), (3, 3), (3, 4), self.client, 7),
            'sarah'  : Shutter('sarah', (1, 6), (1, 5), (3, 5), (3, 6), self.client, 7)
            }
        connected = False
        while not connected:
            try:
                connected = (self.client.connect('192.168.1.105') == 0)
            except OSError as e:
                self.logger.warn("received OSError: {}".format(e.strerror))
                time.sleep(59)

            time.sleep(1)
        self.client.subscribe('home/shutter/+/request')


    def on_connect(self, client, userdata, flags, rc):
        if (rc == 0):
            client.publish('home/shutter/status', 'ONLINE', 2, True)

        self.logger.info("Connection Established")

    def on_disconnect(self, client, userdata, rc):
        client.publish('home/shutter/status', 'OFFLINE', 2, True)
        self.logger.info("Connection Terminated")

    def on_message(self, client, userdata, message):

        if (not message.payload): return

        client.publish(message.topic, None, retain = True, qos = 2)

        topic_list = message.topic.split('/')
        if topic_list[:2] == ['home', 'shutters'] and topic_list[3] == 'request':
            shutter_name = topic_list[2]
            action = str(message.payload.decode('utf-8'))
            self.logger.info("Message received: {} {}".format(
                    shutter_name, action
                )
            )
            if (action == "OPEN"):
                self.shutters[shutter_name].open()
            elif (action == "CLOSE"):
                self.shutters[shutter_name].close()
            elif (action == "STOP"):
                self.shutters[shutter_name].stop()
            elif (action == "HALF"):
                self.shutters[shutter_name].half()
        else:
            self.logger.warn(
                "Received message on unknown topic: {}".format(
                    str(message.topic)
                )
            )

    def mainloop(self):
        self.logger.info("Entering Loop")
        self.client.loop_forever()

def main():
    logger = logging.getLogger('shutter')
    logger.addHandler(JournalHandler())
    logger.setLevel(logging.INFO)
    logger.info("Starting Shutter Controller")
    app = Mqtt_Shutter(logger=logger)
    app.mainloop()

if __name__ == '__main__':
    main()
