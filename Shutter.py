#!/usr/bin/env python3 

from HBridge import HBridge

class Shutter:
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
    print(args)
    if 'close' in args:
        living.close()
    elif 'open' in args:
        living.close()
    elif 'stop' in args:
        living.stop()

if __name__ == '__main__':
    main()
