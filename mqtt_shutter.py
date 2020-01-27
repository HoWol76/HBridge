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

def on_connect(client, userdata, rc):



def main():

    client = mqtt.Client(
        'rpi-shutter-controller',
        userdata={
            'username': 'rpi2',
            'password': 'dbx-102'
        },
        protocol = mqtt.MQTTv311
    )

    Shutters = {
        'living' : Shutter('living', (0, 1), (0, 2), (0, 3), (0, 4), client, 14),
        'stairs' : Shutter('stairs', (1, 1), (1, 2), (1, 3), (1, 4), client, 12),
        'master' : Shutter('master', (3, 1), (3, 2), (3, 3), (3, 4), client, 12),
        'sarah'  : Shutter('sarah', (0, 5), (1, 5), (3, 5), (0, 6), client, 12)
        }