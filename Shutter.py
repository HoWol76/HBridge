#!/usr/bin/env python3

from HBridge import HBridge

class Shutter:
    """
    Configures an H-Bridge specifically for the roller shutter.
    Hides the H-Bridges brake configuration, we don't need it.
    """
    def __init__(self, forward_in, forward_out, reverse_in, reverse_out):
        self.hbridge = HBridge(forward_in, forward_out, reverse_in, reverse_out)
    def close(self):
        self.hbridge.forward()
    def open(self):
        self.hbridge.reverse()
    def stop(self):
        self.hbridge.off()

def main():
    import optparse
    parser = optparse.OptionParser("Manual Control of the Shutters")
    (options, args) = parser.parse_args()
    living = Shutter((0, 1), (0, 2), (0, 3), (0, 4))
    stairs = Shutter((1, 1), (1, 2), (1, 3), (1, 4))
    if 'close' in args:
        living.close()
        stairs.close()
    elif 'open' in args:
        living.open()
        stairs.open()
    elif 'stop' in args:
        living.stop()
        stairs.stop()

if __name__ == '__main__':
    main()
