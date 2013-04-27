unity-3d-package-extract
========================

Simple method to extract .unitypackage files, since it seems to blow up unity on occasion and  if you have lots of assets there is a good chance you just want to pull in what you need...

Install with [chocolatey](http://chocolatey.org/)

	cinst upackx

usage: 

	upackx [-i] packageFile.unitypackage [-o] directoryName
	extracts packageFile.unitypackage to the folder 'directoryName'