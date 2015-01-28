#Author: Heera
#Date: 17-05-2013
from datetime import datetime
import smtplib
from email.mime.multipart import MIMEMultipart
from email.mime.text import MIMEText
from email.parser import Parser
import smtplib, os, re, sys, glob, string, datetime
from email.MIMEMultipart import MIMEMultipart
from email.MIMEBase import MIMEBase
from email.MIMEText import MIMEText
from email import Encoders
from UserString import MutableString
import json

username ='info@travelyaari.com'
password = 'mantisyaari.com'
#send_to='heera.jaiswal@travelyaari.com'
html_file='/home/heera/data_platform/env/kunkka/kunkka/email_template.html'
    
def sendmail(to_,sub_,content,attachmentname=None):
    try:
        if type(to_)==list:
            to_=",".join(to_)
        global username,password,html_file
        html=""
        from_=username
        # with open(html_file,"rb") as f:
        #     html=f.read()
        #     f.close()
        # html=html.replace("$body",content)
        html=content
        msgtext = html.replace('<b>','').replace('</b>','').replace('<br>',"\r").replace('</br>',"\r").replace('<br/>',"\r").replace('</a>','')        
        msgtext = re.sub('<.*?>','',msgtext)

        msg = MIMEMultipart()
        msg.preamble = 'This is a multi-part message in MIME format.\n'
        msg.epilogue = ''

        body = MIMEMultipart('alternative')
        body.attach(MIMEText(msgtext))
        body.attach(MIMEText(html, 'html'))
        msg.attach(body)

        if attachmentname: #DO WE HAZ ATTACHMENT?
            f = attachmentname
            part = MIMEBase('application', "octet-stream")
            part.set_payload( open(f,"rb").read() )
            Encoders.encode_base64(part)
            part.add_header('Content-Disposition', 'attachment; filename="%s"' % os.path.basename(f))
            msg.attach(part)

        msg.add_header('From', from_)
        msg.add_header('To', to_)    
        msg.add_header('Subject', sub_)        

        server = smtplib.SMTP('smtp.gmail.com:587')
        server.starttls()
        server.login(username,password)
        server.sendmail(msg['From'], to_.split(","), msg.as_string())
        server.quit()

    except Exception as ex:
        print ex


