@echo off
HostDriver.exe --UserName="BH-E6520\test_user" --Password="Pass@word1" --ContainerDir="C:\IronFoundry\warden\containers" --ServiceName="TST_SVC" -- CpuRateLimit=0 --MemoryLimit=0 --Command="cmd.exe" --WorkingDirectory="" --Arguments="/c ping 127.0.0.1 -n 10 -w 100"