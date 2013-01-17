yum update -y
yum install wget -y

shutdown -r now

#set script to restart#
wget Step2.sh
chmod +x Step2.sh

yum update -y
cd /usr/src

wget http://apps.weavver.internal/elasticsymphony/Subversion.sh
sh Subversion.sh

#cd /usr/src/install apr-util