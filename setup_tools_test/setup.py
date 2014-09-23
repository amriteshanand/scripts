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
    packages = packages)
