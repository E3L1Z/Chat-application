# Genral
This is my attempt at an chat application. 
**IT IS NOT MEANT TO BE SECURE** thus do not use it for any real applications.
However do feel free to use it in educational purposes.

# Techincal information
Every message consists of a package. That package is 1024 bytes long and consists of the following data.

| Method (1) | Destination IP (4) | Destination port (2) | Source IP (4) | Message index (4) | Data type (1) | Data (1008) |
| ---------- | ------------------ | -------------------- | ------------- | ----------------- | ------------- | ----------- |

## Method
Method can have following values:
| Value | Meaning | More information |
| ----- | ------- | ---------------- |
| 0 | Message | Used to send a message |
| 1 | Close/Quit | Used to either tell clients that the server is closing or that a client is leaving |
| 2 | Fetch users | Get all online users on server |

## Destination IP and port
Used to tell who the packet is suposed to be delivered.
On default both are same as servers.
If destination port is the same as servers then send packet to everyone who has the same ip as destination ip. Except if destination IP is same as servers then send packet to everyone.
Otherwise send packet to user whose port and ip matches that of destination

## Source IP
Used to tell who sent packet. If packet originates from server then source IP will be left empty

## Message index
If message length is lower or equal to 1008 bytes then index will be sent to 0.
However if message is longer than 1008 index will be equivelant to 1 increasing by one every time next part of the message is sent.
If message was longer than 1008 bytes then on the last part of the message index will be equal to { 255, 255, 255, 255 }.

