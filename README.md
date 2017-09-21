# Watson Syslog Server

[![][nuget-img]][nuget]

[nuget]:     https://www.nuget.org/packages/BigQ.dll
[nuget-img]: https://badge.fury.io/nu/Object.svg

## Simple Syslog Server in C#

Watson Syslog Server will automatically start using a default configuration listening on UDP/514 and storing log files in the ```logs\``` directory.  If you wish to change this, create a file called ```syslog.json``` with the following structure:
```
{
  "Version": "Watson Syslog Server v1.0.0",
  "UdpPort": 514,
  "DisplayTimestamps": true,
  "LogFileDirectory": "logs\\",
  "LogFilename": "log.txt",
  "LogWriterIntervalSec": 10
}
```

## Help or Feedback

Do you need help or have feedback?  Contact me at joel at maraudersoftware.com dot com or file an issue here!

## New in v1.0.0

- Initial release
 
## Starting the Server

Build/compile and run the binary.

## Running under Mono

This app should work well in Mono environments.  It is recommended that when running under Mono, you execute the containing EXE using --server and after using the Mono Ahead-of-Time Compiler (AOT).
```
mono --aot=nrgctx-trampolines=8096,nimt-trampolines=8096,ntrampolines=4048 --server myapp.exe
mono --server myapp.exe
```

## Version History

Notes from previous versions (starting with v1.0.0) will be moved here.
