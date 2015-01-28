#Author: Heera
#Date: 2014-09-01
#Description: helpers for creating jobs

import multiprocessing as mp
import Queue
from multiprocessing.managers import SyncManager
from datetime import datetime,timedelta
import time
import pickle
AUTHKEY= "60c05c632a2822a0a877c7e991602543"
PORTNUM = 8004 #Preffered port
IP="10.66.60.90"#'127.0.0.1'



class JobsWaiter(object):	
	# FAILED 	=-1
	# SUCCESS =0	
	ADDED 	=1
	WAITING =2

	@staticmethod
	def callback(response,**kwrd):	
		manager=JobsManager() #make_client_manager(IP,PORTNUM,AUTHKEY)
		# status_obj=manager.get_sync_data()
		req=response["request"]
		#process_id=req[2]["process_id"]
		waiter=JobsWaiter(manager,kwrd["name"])
		waiter.done_task(req)

	
	def __init__(self,manager,name):		
		#self.waiter_queue=manager.get_waiter(name)		
		self.wait_list=manager.get_wait_list(name)		
		
		self.tasks=[]		
		self.manager=manager
		self.name=name		
	
	def get_callback_job(self):
		return ("helpers.jobs","JobsWaiter.callback",{"name":self.name})

	def add_job(self,obj):
		self.manager.add_job(obj,	
							callback_list=[self.get_callback_job()])	
		#self.tasks.append(obj)
		self.wait_list[pickle.dumps(obj)]=JobsWaiter.ADDED
		
	def done_task(self,obj):
		#self.waiter_queue.put(obj)		
		del self.wait_list[pickle.dumps(obj)]

	def wait(self,timeout=6*60*60,finish=True):
		dt_start=datetime.now()
		curr_timeout=timeout
		print "waiting in sink"
		try:			
			time.sleep(1)
			print self.wait_list.keys()
			for key in self.wait_list.keys():
				self.wait_list[key]=JobsWaiter.WAITING

			while len(self.wait_list.keys())>0 and curr_timeout>0:
				
				# try:
				# 	obj=self.waiter_queue.get(True,curr_timeout)
				# except Queue.Empty:
				# 	break
				#if obj in self.tasks:
				#	del self.tasks[self.tasks.index(obj)]
				diff = datetime.now() - dt_start				
				curr_timeout = timeout - int(diff.total_seconds())
				time.sleep(1)
				print "timeout=%d" % (curr_timeout)
			self.manager.delete_waiter(self.name)
			print self.wait_list.keys()
			print self.manager.get_wait_list(self.name)		
		except Exception as e:
			print e

	def get_tasks(self):
		return self.tasks


##-----------------------------------New API-------------------------------------###
class JobsManager(SyncManager):
	"""

	"""
	def __init__(self):
		super(JobsManager, self).__init__(address=(IP, PORTNUM), authkey=AUTHKEY)
		self.connect()
		print 'Client connected to %s:%s' % (IP, PORTNUM)

JobsManager.register('add_job')	
JobsManager.register('get_sync_data')	
JobsManager.register('get_callbacks_dict')	
JobsManager.register('get_waiter')	
JobsManager.register('delete_waiter')
JobsManager.register('get_wait_list')	

##-----------------------------------------------------------------------------##



## Older API ####
class JobsConsumer(SyncManager):
	pass
JobsConsumer.register('add_job')	
JobsConsumer.register('get_sync_data')	
JobsConsumer.register('get_callbacks_dict')	
JobsConsumer.register('get_waiter')	
JobsConsumer.register('delete_waiter')
JobsConsumer.register('get_wait_list')	
		
def make_client_manager(ip, port, authkey):
	""" Create a manager for a client. This manager connects to a server on the
		given address and exposes add_job method to add job.
		Return a manager object.
	"""
	JobsConsumer.register('add_job')	
	JobsConsumer.register('get_sync_data')	
	JobsConsumer.register('get_callbacks_dict')	
	JobsConsumer.register('get_waiter')	
	JobsConsumer.register('get_wait_list')	
	
	JobsConsumer.register('delete_waiter')
	
	
	manager = JobsConsumer(address=(ip, port), authkey=authkey)
	manager.connect()

	print 'Client connected to %s:%s' % (ip, port)
	return manager
