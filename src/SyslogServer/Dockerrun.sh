if [ -z "${IMG_TAG}" ]; then
  IMG_TAG='v2.0.0'
fi

echo Using image tag $IMG_TAG

if [ ! -f "syslog.json" ]
then
  echo Configuration file syslog.json not found.
  exit
fi

# Items that require persistence
#   syslog.json
#   logs/

# Argument order matters!

docker run \
  -p 514:514/udp \
  -t \
  -i \
  -e "TERM=xterm-256color" \
  -v ./syslog.json:/app/syslog.json \
  -v ./logs/:/app/logs/ \
  jchristn/syslogserver:$IMG_TAG

