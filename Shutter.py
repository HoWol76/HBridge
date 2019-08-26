#!/usr/bin/env python3
# Script to control roller shutters.

from HBridge import HBridge
import argparse

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

Shutters = {
        'living' : Shutter((0, 1), (0, 2), (0, 3), (0, 4)),
        'stairs' : Shutter((1, 1), (1, 2), (1, 3), (1, 4)),
        'master' : Shutter((3, 1), (3, 2), (3, 3), (3, 4)),
        'sarah'  : Shutter((0, 5), (1, 5), (3, 5), (0, 6))
        }


def func_open(args):
    for s in args.Shutter:
        if s in Shutters.keys():
            if args.verbose:
                print("Opening Shutter {s}".format(s=s))
            Shutters[s].open()
        else:
            if args.verbose:
                print("{} not recognised as Shutter".format(s))
                print("Please select one of {}".format(list(Shutters.keys())))


def func_close(args):
    for s in args.Shutter:
        if s in Shutters.keys():
            if args.verbose:
                print("Closing Shutter {s}".format(s=s))
            Shutters[s].close()
        else:
            if args.verbose:
                print("{} not recognised as Shutter".format(s))
                print("Please select one of {}".format(list(Shutters.keys())))


def func_stop(args):
    for s in args.Shutter:
        if s in Shutters.keys():
            if args.verbose:
                print("Stopping Shutter {s}".format(s=s))
            Shutters[s].stop()
        else:
            if args.verbose:
                print("{} not recognised as Shutter".format(s))
                print("Please select one of {}".format(list(Shutters.keys())))


def main():
    parser=argparse.ArgumentParser(description="Testing Script for argparse")
    parser.add_argument('--verbose', '-v', help='Verbose', action='store_true')
    shutters=argparse.ArgumentParser(add_help=False)
    shutters.add_argument('Shutter', nargs='*', help='Shutters to operate (default: all)',
            default=list(Shutters.keys()))
    action = parser.add_subparsers(help='Action')
    ac_open = action.add_parser('open', parents=[shutters], help="Open Shutters")
    ac_close = action.add_parser('close', parents=[shutters], help="Close Shutters")
    ac_stop = action.add_parser('stop', parents=[shutters], help="Stop Shutters")
    ac_open.set_defaults(func=func_open)
    ac_close.set_defaults(func=func_close)
    ac_stop.set_defaults(func=func_stop)
    args = parser.parse_args()
    
    args.func(args)


if __name__ == '__main__':
    main()
