@echo off

IF "%1" == "" GOTO :Usage

if not exist syslog.json (
  echo Configuration file syslog.json not found.
  exit /b 1
)

REM Items that require persistence
REM   syslog.json
REM   logs/

REM Argument order matters!

docker run ^
  -p 514:514/udp ^
  -t ^
  -i ^
  -e "TERM=xterm-256color" ^
  -v .\syslog.json:/app/syslog.json ^
  -v .\logs\:/app/logs/ ^
  jchristn/syslogserver:%1

GOTO :Done

:Usage
ECHO Provide one argument indicating the tag.
ECHO Example: dockerrun.bat v2.0.0
:Done
@echo on
