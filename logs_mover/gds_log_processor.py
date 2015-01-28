#Author: Heera
#Date: 2014-07-22
#Description: sync log files and archive them

import ConfigParser
import subprocess
import logging
from datetime import datetime
from datetime import timedelta
from pytz import timezone
config=ConfigParser.ConfigParser()
#config.read("config.ini")
config.read("C:\Automate\logs_mover\config.ini")

cur_date=datetime.now()#timezone("GMT"))
section="production"
from subprocess import Popen
RSYNC_DIR=config.get(section,"RSYNC_DIR")
SSH_FILE=config.get(section,"SSH_FILE")
FILES_DIR=config.get(section,"FILES_DIR")
DESTINATION=config.get(section,"DESTINATION")
DESTINATION_DIR=config.get(section,"DESTINATION_DIR")
RSYNC_LOG=config.get(section,"RSYNC_LOG")
BACK_TIME=int(config.get(section,"BACK_TIME"))
SOURCE_DIR=config.get(section,"SOURCE_DIR")
INTERVAL=int(config.get(section,"INTERVAL"))
FILE_FORMAT=config.get(section,"FILE_FORMAT")
logging.basicConfig(filename=RSYNC_LOG+cur_date.strftime("log_%Y_%m_%d")+".txt")
logger = logging.getLogger("rsync_log")
logger.setLevel(20) #INFO LEVEL
#print ["rsync_file.cmd",SSH_FILE,FILES_DIR,"*",DESTINATION,DESTINATION_DIR]

def getFileListRE(dt):
    file_list=[FILES_DIR+(dt-timedelta(minutes=i)).strftime(FILE_FORMAT) for i in range(INTERVAL)]
    return " ".join(file_list)
    
def sync_files(name):
    logger.info("Sending Files:")
    try:
        p = subprocess.Popen(["rsync_file.cmd",SSH_FILE,SOURCE_DIR,name,DESTINATION,DESTINATION_DIR],shell=True,cwd=RSYNC_DIR,stdout=subprocess.PIPE,stderr=subprocess.PIPE,)
        out, err = p.communicate()
        #logger.info(out)
        #print out
        logger.info("Sent")
        if err and len(err)>0:
            logger.error(err)
    except e as Exception:
        logger.error(e)


date_files=cur_date - timedelta(minutes=BACK_TIME)
logger.info(date_files.strftime("%Y_%m_%d_%H_%M"))
sync_files(getFileListRE(date_files))#date_files.strftime("*%Y_%m_%d*"))




