import HBridge

class Shutter:
    def __init__(self, forward_in, forward_out, reverse_in, reverse_out):
        self.hbridge = HBridge(forward_in, forward_out, reverse_in, reverse_out)
    def close(self):
        self.hbridge.forward()
    def open(self):
        self.hbridge.reverse()
    def stop(self):
        self.hbridge.off()
