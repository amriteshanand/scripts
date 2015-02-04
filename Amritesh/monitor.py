#--------------------------------------------------------------------
# Script summary:
# This script is used to generate invoice mails for sub-Agents.
# Usage: subagentinvoice.py.
# Functions:
#	check_db - Gets DB info and creates/sends the mail
# 	process - intiates DB connectio, calls check_db, closes the connection.
# 	CreateSQLInstance - Creats Connection
#--------------------------------------------------------------------

import os
import sys
import time
import _mssql 
import string
from datetime import datetime
from smtplib import SMTP
from mailer import Mailer
from mailer import Message
import ConfigParser
import ast

# Template for mail includes To,From and Subject.
message = Message(From="amritesh.anand@travelyaari.com",
                  To=["amritesh.anand@travelyaari.com"],
                  CC=["amritesh.anand@travelyaari.com"],
                  #To=["amritesh.anand@travelyaari.com"],
                  #CC=["amritesh.anand@travelyaari.com"],
                  Subject="Invoice Report")

#config = ConfigParser.ConfigParser()
#config.read("config.ini")

# Default mailer for sending mails
#mailer = config.get("Mailer", "mailer")
sender = Mailer(host="smtp.gmail.com", port=587, use_tls=True, usr="info@travelyaari.com", pwd="mantisyaari.com")


fields = [
"Voucher_ID"
,"voucher_no"
,"Agent_Name"
,"Generated_Date"
,"From"
,"To"
,"Gross_Amount"
,"Discount"
,"Commission"
,"Cancel_Charges"
,"SRS_Fee"
,"SRS_Agent_Comm"
,"Collection_Amount"
]
def CreateSummary(rows):
    try:
        str1 = "<B>Sub Agent Invoice Details:</B><BR/><BR/><TABLE Border=1>"
        str2 = "</TABLE>"
        str3 = "<TR>"
        for field in fields:
        	if field == "Voucher_ID":
        		continue
        	str3 = str3 + "<TH>" + field + "</TH>"
        str4 = str3 + "<TH>Link</TH>" + "</TR>"
        str5 = ""
        total_collection = 0
        for row in rows:
          str5 += "<TR>"
          for field in fields:
          	if field == "Voucher_ID":
          		continue
          	str5 += "<TD>" + str(row[field]) + "</TD>"
          str5 += "<TD><a href=\"http://iamgds.com/ShowReports.aspx?Mode=AI&ToVoucherNo="+str(row["Voucher_ID"])+"\">View Invoice</a></TD>" + "</TR>"
          total_collection += row["Collection_Amount"]          
        return str1 + str4 + str5 + str2 + "<BR/> <B> Total : </B> " + str(total_collection)
    except:
        #Program.WriteLog(ex.Message)
        return "Error"

def check_db():
	global conn
	print "[%s]Checking DB..." % (datetime.now())
	try:
		try:
			conn.execute_query("dbo.spSubAgent_GenerateVouchers")
			rows = conn
		except:
			print "There was an error in query!"
			return False

		message.Html = CreateSummary(rows)

		sender.send(message)	
				
	except KeyboardInterrupt:
		return True

# Creates Instance of SerenaDB		
def CreateSQLInstance():
    global conn
    conn = _mssql.connect(server='gdsdb', user='agn', password='t0w3r47556br!dg3', database='GDS')
    print ("\nStart::Main::Connection Successful\n")
				

def process():
	global conn
	CreateSQLInstance()
	check_db()
	conn.close()
	print "Connection Closed"
	

if __name__ == "__main__":
	# Use for debugging
	#process()
	while True:
		now = datetime.now()
		diff = (24 - now.hour) * 60 *60 + (60 - now.minute) * 60 - now.second
		for i in range(diff+10*60*60, 0, -1):
			print "\rSleeping for %s seconds..." % i,
			sys.stdout.flush()
			time.sleep(1)
		print
		message.Subject = ""
		message.Html = ""
		try:
			process()
		except KeyboardInterrupt:
			sys.exit()
		except:
			try:
				message.Subject = " InvoiceReport "
				message.Subject += datetime.now().strftime("%d-%m")
				message.To = "amritesh.anand@travelyaari.com"
				message.CC = "amritesh.anand@travelyaari.com"
				message.Html = "Invoice Script stopped Working"
				print "something wrong with the script"
				sender.send(message)
			except:
				print "Network problem"