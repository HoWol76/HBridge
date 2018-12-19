#!/usr/bin/env python3

import piplates.RELAYplate as RELAY

class HBridge:
    def __init__(self, forward_in, forward_out, reverse_in, reverse_out):
        self.forward_in = forward_in
        self.forward_out = forward_out
        self.reverse_in = reverse_in
        self.reverse_out = reverse_out
        self.nevertogether = {
            self.forward_in : self.reverse_out,
            self.reverse_out : self.forward_in,
            self.forward_out : self.reverse_in,
            self.reverse_in : self.forward_out
        }
    def forward(self):
        self.__relayOFF(self.reverse_in)
        self.__relayOFF(self.reverse_out)
        self.__relayON(self.forward_in)
        self.__relayON(self.forward_out)
    def reverse(self):
        self.__relayOFF(self.forward_in)
        self.__relayOFF(self.forward_out)
        self.__relayON(self.reverse_in)
        self.__relayON(self.reverse_out)
    def off(self):
        self.__relayOFF(self.forward_in)
        self.__relayOFF(self.forward_out)
        self.__relayOFF(self.reverse_in)
        self.__relayOFF(self.reverse_out)
    def brake(self):
        self.__relayOFF(self.forward_in)
        self.__relayOFF(self.reverse_in)
        self.__relayON(self.forward_out)
        self.__relayON(self.reverse_out)
    def __relayOFF(self, channel):
        RELAY.relayOFF(channel[0], channel[1])
    def __relayON(self, channel):
        self.__relayOFF(self.nevertogether[channel])
        RELAY.relayON(channel[0], channel[1])

def main():
    import optparse
    parser = optparse.OptionParser("Manual Control of the Shutters")
    (options, args) = parser.parse_args()
    living = Shutter((0, 1), (0, 2), (0, 3), (0, 4))
    print(args)

if __name__ == '__main__':
    main()
