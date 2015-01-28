#Author: Heera
#Date: 2014-08-27
#Description: DB Adapter

import pymssql
import _mssql

class DB(object):
	"""
		Author: Heera
		Date: 2014-08-27
		Description: Process stations data from api, param=str_response
	"""
	def __init__(self,server,db,user,password):
		self.server		= server
		self.db 		= db
		self.user 		= user
		self.password 	= password
		
	# @staticmethod
	# def __log_handler__(msgstate, severity, srvname, procname, line, msgtext):
		# """
		# Our custom log handler -- It simpy prints a string to stdout assembled from
		# the pieces of information sent by the server.
		# """
		# print("Log: msgstate = %d, severity = %d, procname = '%s', "
			  # "line = %d, msgtext = '%s'" % (msgstate, severity, procname,
											 # line, msgtext))
										 
	def execute_query(self,str_query,params=None,commit=False,Fetch=True):
		result=None
		with pymssql.connect(self.server, self.user, self.password, self.db) as conn:
			conn.autocommit(commit)
			with conn.cursor(as_dict=True) as cursor:
				cursor.execute(str_query,params)
				if Fetch==True:
					result=cursor.fetchall()
		return result

	def execute_dml_bulk(self,str_query,list_data):
		with pymssql.connect(self.server, self.user, self.password, self.db) as conn:			
			with conn.cursor(as_dict=True) as cursor:
				cursor.executemany(str_query,list_data)				
				conn.commit()
	
	def execute_dml(self,str_query):
		with pymssql.connect(self.server, self.user, self.password, self.db) as conn:
			conn.autocommit(True)
			with conn.cursor(as_dict=True) as cursor:
				cursor.execute(str_query)				
				conn.commit()				

	def execute_sp(self,sp_name,list_params,commit=False):
		data_set=None
		with pymssql.connect(self.server, self.user, self.password, self.db) as conn:
			conn.autocommit(commit)
			with conn.cursor(as_dict=True) as cursor:
				result=cursor.callproc(sp_name,list_params)
				data_set=[result]
				while cursor.nextset():
					result=cursor.fetchall()
					data_set.append(result)
		return data_set
		
	# def execute_generic(self,str_sql,params):
		# result=None
		# print 
		# with _mssql.connect(server=self.server, user=self.user, password=self.password, database=self.db) as conn:
			#conn.set_msghandler(self.__log_handler__)
			# conn.execute_scalar(str_sql,params)
			# while conn.nextresult():
				# print 
			# print "result: ",result
		# return result
			
		

if __name__=='__main__':
	db=DB("pulldb3","Pull_Parveen_2","agn","t0w3r47556br!dg3")
	#print db.execute_query("""exec process_data %d""",(46300386,),commit=True)
	#db.execute_dml_bulk("insert into test(sr_no,name,value) values(%d,%s,%d)",[(123,'Heera',1)])
	#print db.execute_query("select * from test with (nolock)")
	db.execute_sp("process_data",(46300386,),commit=True)
	#db.execute_generic("""exec process_data %d""",(46300386,))
	pass

