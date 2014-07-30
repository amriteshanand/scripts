@ECHO OFF
REM *****************************************************************
REM
REM CWRSYNC.CMD - Batch file template to start your rsync command (s).
REM
REM By Tevfik K. (http://itefix.no)
REM *****************************************************************

REM Make environment variable changes local to this batch file
SETLOCAL

REM ** CUSTOMIZE ** Specify where to find rsync and related files (C:\CWRSYNC)
SET CWRSYNCHOME=%PROGRAMFILES(X86)%\CWRSYNC

REM Set CYGWIN variable to 'nontsec'. That makes sure that permissions
REM on your windows machine are not updated as a side effect of cygwin
REM operations.
SET CYGWIN=nontsec

REM Set HOME variable to your windows home directory. That makes sure 
REM that ssh command creates known_hosts in a directory you have access.
REM SET HOME=%HOMEDRIVE%%HOMEPATH%

REM Make cwRsync home as a part of system PATH to find required DLLs
SET CWOLDPATH=%PATH%
REM SET PATH=%CWRSYNCHOME%\BIN;%PATH%

REM Windows paths may contain a colon (:) as a part of drive designation and 
REM backslashes (example c:\, g:\). However, in rsync syntax, a colon in a 
REM path means searching for a remote host. Solution: use absolute path 'a la unix', 
REM replace backslashes (\) with slashes (/) and put -/cygdrive/- in front of the 
REM drive letter:
REM 
REM Example : C:\WORK\* --> /cygdrive/c/work/*
REM 
REM Example 1 - rsync recursively to a unix server with an openssh server :
REM
REM       rsync -r /cygdrive/c/work/ remotehost:/home/user/work/
REM
REM Example 2 - Local rsync recursively 
REM
REM       rsync -r /cygdrive/c/work/ /cygdrive/d/work/doc/
REM
REM Example 3 - rsync to an rsync server recursively :
REM    (Double colons?? YES!!)
REM
REM       rsync -r /cygdrive/c/doc/ remotehost::module/doc
REM
REM Rsync is a very powerful tool. Please look at documentation for other options. 
REM

REM ** CUSTOMIZE ** Enter your rsync command(s) here

set "param1=%~1"
REM Disable rsync since dps server not running
REM rsync -ctvz /cygdrive/c/HostingSpaces/admin/api.mantistechnologies.com/wwwroot/Logs/*%param1%* gruffi@dps:/home/gruffi/logs/

set "SSH_FILE=%~1"
set "SOURCE_DIR=%~2"
set "FILES=%~3"
set "DESTINATION=%~4"
set "DESTINATION_DIR=%~5"

ECHO %FILES%
ECHO %DESTINATION_DIR%

REM C:\rsync\rsync.exe -rcvztp --remove-source-files -e "/cygdrive/c/rsync/ssh.exe -o StrictHostKeyChecking=no -i /cygdrive/c/rsync/beta_ty.pem"  /cygdrive/c/Users/MY/Documents/beta_ty_key/test.txt ec2-user@ec2-175-41-167-239.ap-southeast-1.compute.amazonaws.com:/data2/gds_logs/dev/


C:\rsync\rsync.exe -rcvzta --min-size=1B --remove-source-files -e "/cygdrive/c/rsync/ssh.exe -o StrictHostKeyChecking=no -i %SSH_FILE%"  %FILES% %DESTINATION%:%DESTINATION_DIR%


FOR %%F IN ("%SOURCE_DIR%*") DO (IF "%%~zF" == "0" del %%~fF )
