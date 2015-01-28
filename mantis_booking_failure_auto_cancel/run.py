#Author: Heera
#Date: 2015-01-05
#Description: Mantis Booking Failure auto cancel

"""
Cancel tickets in crs which are booked in crs only 
but not got updated gds
Should call this crs special cancel api within 30 mins of hold
should send report to b2c 
"""
from helpers import db,email_sender
import httplib2
import json

## Mantis API Configuration
mantis_special_cancel_url="http://216.185.100.204/crsapi/Service.svc/FaultBooking_AutoCancel?"
str_key="7816c388fff4387a490ba8d9372a2ff8"
##

def cancel_mantis_ticket(lock_ref,agent_id="2707"):	
	h=httplib2.Http()
		#proxy_info = httplib2.ProxyInfo(socks.PROXY_TYPE_HTTP, proxy_ip, proxy_port))
	path = mantis_special_cancel_url \
			+ '&strLockKey=' + lock_ref \
			+ '&intAgentID=' + agent_id \
			+ '&strKeyCode=' + str_key

	(resp_headers, content) = h.request(path, "GET")
	if resp_headers["status"]=="200":
		return content
	else:
		raise Exception("Invalid Response")#str(resp_headers))

def update_mantis_cancel_in_gds(cancel_status):
	gds_db = db.DB("gdsdb","gds","agn","t0w3r47556br!dg3")
	for booking_id in iter(cancel_status):
		print booking_id
		if cancel_status[booking_id]==True:
			gds_db.execute_query("""
					UPDATE bookings WITH (ROWLOCK) set status='FC' WHERE booking_id=%d
				""" % (booking_id,),commit=True,Fetch=False)

def table_to_html(table):
	headers=[]
	str_body='<tbody>'
	for row in table:
		if len(headers)==0:
			headers=row.keys()
		str_row='<tr>'
		for item in row:
			str_row+='<td style="border:1px solid">'+str(row[item])+'</td>'
		str_row+='</tr>'
		str_body+=str_row
	str_body+='</tbody>'
	return '<table>'+'<tr><th style="border:1px solid">'+'</th><th style="border:1px solid">'.join(headers)+'</th></tr>'+str_body+'</table>'
			

def send_cancel_report_to_team(cancel_status):
	header="Mantis Booking Failures"
	gds_db = db.DB("gdsdb","gds","agn","t0w3r47556br!dg3")
	db_response=gds_db.execute_sp("GET_BOOKING_FAILURES_BY_ID",
		(
			",".join([str(key) for key in cancel_status.keys()]), #Mantis provider
		),commit=True)	
	if len(db_response)>1:
		print db_response
		email_content='<html><body><h1>'+header+'</h1><div>'+table_to_html(db_response[1])+'</div>'+'</body></html>'
		print email_content
		email_sender.sendmail('heera.jaiswal@travelyaari.com','Straja: Provider Booking Failures',email_content)


if __name__=='__main__':	
	gds_db = db.DB("gdsdb","gds","agn","t0w3r47556br!dg3")
	db_response=gds_db.execute_sp("GET_PROVIDER_BOOKING_FAILURES",
		(
			15, #Mantis provider
		),commit=True)	
	if len(db_response)>1:
		request,failed_bookings = db_response		
		cancel_status={}
		for booking in failed_bookings:
			try:
				cancel_response=json.loads(cancel_mantis_ticket(booking["LOCK_REFERENCE_NO"]))				
				print cancel_response
				if cancel_response["FaultBooking_AutoCancelResult"] =="Cancelled Successfully" :
					cancel_status[booking["BOOKING_ID"]]=True
				elif cancel_response["FaultBooking_AutoCancelResult"] =="Cancellation Denied":
					cancel_status[booking["BOOKING_ID"]]=False
				else:
					cancel_status[booking["BOOKING_ID"]]=False

			except Exception as e:
				print e
				cancel_status[booking["BOOKING_ID"]]=False				
		print cancel_status
		cancel_status[6394126]=True
		update_mantis_cancel_in_gds(cancel_status)
		send_cancel_report_to_team(cancel_status)
	else:
		print "No such booking is found."

