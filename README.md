# General
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

## Data type
Tells what type of message we are sending.
Currently the only supported data type is ASCII text which will give data type value of 0.


# Commands
List of all the commands and a bit of what they do.

## Spawn
`spawn [-P*port*]?`

Used to spawn in a server.
  -P (-P\*port\* or -P \*port\*) - Can be used to specify which port server will be open in (default 2500).

## Close
`close`

Used to close current server.
Sends package with metho equal to 0 to every client online. This then tells the client to act the same way as if user gave command [quit](#quit)

## Join
`join *destinationIP* [-P*port*]?`

Join server on destinationIP on port
    -P (-P\*port\* or -P \*port\*) - Can be used to specify which port to connect to server (default 2500).

## Quit
`quit`

Leaves server.
Send a packet to server with method eequal to 0 which tells server the client is leaving.

## Msg
`msg [-d*destinationIP*]? [-P*port*]?`

Send message to user with ip equal to destinationIP and port equal to port. 
If port is equal to servers port then send packet to every user on ip destinationIP.
If port and destinationIP is the same as servers send message to every user online on server.
  -d (-d\*destinationIP\* or -d \*destinationIP\*) - IP which packet will be sent to. Can also be 127.0.0.1 or localhost (default servers IP).
  -P (-P\*port\*) or -P \*port\*) - port to which packet will be sent to (default servers port).

## Info
`info`

Lists important info such as users own IP, whether user has server online and if so then on which port and if user is conncted to any server and if so then to what port and IP.

## Users
`users`

Sends a package with method 2 that will ask server to provide user with every user online on the server except for the user themselves.

## Kick
`kick *IP*:*port*`

Sends same packet as [close](#close) but only to user with the same IP and port.

## Help
`help`

Shows every possible command and a little bit of information on what they do.
