#install subversion
# pre-req: apr-util

wget http://subversion.tigris.org/downloads/subversion-1.5.5.tar.gz
tar -zxf subversion-*.tar.gz

svn checkout http://svn.apache.org/repos/asf/apr/apr-util/branches/0.9.x apr-util
cd apr-util

yum install gcc -y
./configure
make
make install
