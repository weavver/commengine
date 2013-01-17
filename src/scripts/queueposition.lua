-- Scriptname: voiceprompts.lua
-- This script provides queue position announcement
-- using a symlink to the appropiate queue length file

-- Initialization of variables
api = freeswitch.API()
caller_uuid = argv[1]
-- Queue_name not used presently
queue_name = argv[2]
times = 1
-- The 0.wav file is a very short silence sound file
symlinkcommand = "ln -s -f /var/spool/voiceprompts/0.wav /var/spool/voiceprompts/"..caller_uuid..".wav"
os.execute(symlinkcommand)

while (times<1000) do
  freeswitch.msleep(2000)
  position = api:executeString("uuid_getvar "..caller_uuid.." fifo_position")
  freeswitch.consoleLog("notice","Callers fifo position: "..position.."\n");
  if (string.sub(position,1,4)~="-ERR") then
    position = tonumber(position)
    times = times + 1
    -- We anly announce the position when the caller is number two, as number one may already be on the way to be helped.
    position = position - 1
    if (position > 3) then position = 3 end
    symlinkcommand = "ln -s -f /var/spool/voiceprompts/"..position..".wav /var/spool/voiceprompts/"..caller_uuid..".wav"
    freeswitch.consoleLog("notice","Symlinkcommand: "..symlinkcommand.." result: "..os.execute(symlinkcommand).."\n");
    os.execute(symlinkcommand)
  else times = 1000 end
  freeswitch.msleep(4000)
end
delsymlink = "rm '/var/spool/voiceprompts/"..caller_uuid..".wav'"
result = os.execute(delsymlink)