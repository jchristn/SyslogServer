# Syslog Server

## Simple Syslog Server in C#

Syslog Server will automatically start using a default configuration listening on UDP/514 and storing log files in the `./logs/` directory.  If you wish to change this, create a file called `syslog.json` with the following structure:
```
{
  "UdpPort": 514,
  "DisplayTimestamps": true,
  "LogFileDirectory": "./logs/",
  "LogFilename": "log.txt",
  "LogWriterIntervalSec": 10
}
```

## Starting the Server

Build/compile and run the binary.

## Running in Docker

Refer to the `docker` directory.  A build called `jchristn/syslogserver` has been stored on [Docker Hub](https://hub.docker.com/r/jchristn/syslogserver).
