from setuptools import setup, find_packages
packages=find_packages()
print packages
setup(
    name='setup_tools_test',
    version='0.1',
    description='testin setuptools',
    author='Heera',
    include_package_data = True,
#    package_data={"": ["*.ini"]},
    install_requires=["pymssql"],
    setup_requires=["virtualenv"],
    dependency_links=["https://github.com/heera-jaiswal/scripts/blob/master/setup_tools_test/dist/setup_tools_test-0.1-py2.7.egg"],
    packages = packages)
