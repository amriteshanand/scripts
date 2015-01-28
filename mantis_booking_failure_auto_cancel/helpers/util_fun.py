#from helpers 
import db

GDS_USERNAME="agn"
GDS_PASSWORD="t0w3r47556br!dg3"

def get_process_id(provider_id):
	"""
		return process_id from gdsdb for given provider
	"""
	str_sql="""
	DECLARE @process_id INT=0
	EXEC get_process_id %d,@process_id OUT
	SELECT 	@process_id
	"""	
	gds_db = db.DB("gdsdb","gds",GDS_USERNAME,GDS_PASSWORD)	
	db_response = gds_db.execute_query(str_sql,params=(provider_id,),commit=True)#("get_process_id",(provider_id,process_id))
		
	if len(db_response)>0 and db_response[0]["PROCESS_ID"]>0:
		return db_response[0]["PROCESS_ID"]
	else:
		raise Exception("Process Id found as 0!")

###-----------------------------------------------------------------------####
if __name__=='__main__':
	print get_process_id(1)