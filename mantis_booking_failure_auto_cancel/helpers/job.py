#Author: Heera
#Date: 2014-09-01
#Description: helpers for creating jobs

import multiprocessing as mp
import Queue
from multiprocessing.managers import SyncManager
from datetime import datetime,timedelta
import time
AUTHKEY= "60c05c632a2822a0a877c7e991602543"
PORTNUM = 8004 #Preffered port
IP='127.0.0.1'


class Waiter(object):

	@staticmethod
	def callback(response,**kwrd):	
		manager=JobsManager() #make_client_manager(IP,PORTNUM,AUTHKEY)
		status_obj=manager.get_sync_data()
		req=response["request"]
		#process_id=req[2]["process_id"]
		waiter=Waiter(manager,kwrd["name"])
		waiter.done_task(req)

	
	def __init__(self,manager,name):		
		self.waiter_queue=manager.get_waiter(name)		
		self.tasks=[]		
		self.manager=manager
		self.name=name
		print self.waiter_queue
	
	def get_callback_job(self):
		return ("helpers.job","Waiter.callback",{"name":self.name})

	def add(self,obj):
		self.tasks.append(obj)
		
	def done_task(self,obj):
		self.waiter_queue.put(obj)		

	def wait(self,timeout=6*60*60,finish=True):
		dt_start=datetime.now()
		curr_timeout=timeout
		print "waiting in sink"
		try:			
			while len(self.tasks)>0: 
				obj=None
				try:
					obj=self.waiter_queue.get(True,curr_timeout)
				except Queue.Empty:
					break
				if obj in self.tasks:
					del self.tasks[self.tasks.index(obj)]
				diff = datetime.now() - dt_start
				curr_timeout = timeout - int(diff.total_seconds())
				time.sleep(1)
				print "job done: ",obj
			self.manager.delete_waiter(self.name)
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
##-----------------------------------------------------------------------------##



## Older API ####
class JobsConsumer(SyncManager):
	pass
JobsConsumer.register('add_job')	
JobsConsumer.register('get_sync_data')	
JobsConsumer.register('get_callbacks_dict')	
JobsConsumer.register('get_waiter')	
JobsConsumer.register('delete_waiter')
		
def make_client_manager(ip, port, authkey):
	""" Create a manager for a client. This manager connects to a server on the
		given address and exposes add_job method to add job.
		Return a manager object.
	"""
	JobsConsumer.register('add_job')	
	JobsConsumer.register('get_sync_data')	
	JobsConsumer.register('get_callbacks_dict')	
	JobsConsumer.register('get_waiter')	
	JobsConsumer.register('delete_waiter')
	
	
	manager = JobsConsumer(address=(ip, port), authkey=authkey)
	manager.connect()

	print 'Client connected to %s:%s' % (ip, port)
	return manager
