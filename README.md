# CryptoStringDeobber
A deobfusctaor based on dnpatch and dnlib to deobfuscate https://www.ssware.com/cryptoobfuscator/obfuscator-net.htm
Currently threre is limited support to what apps this can deob, at the moment, it is targeting winform apps, although other kinds of crypto obed assemblies will still deob.
The program leverages SnD's SimpleNameDeobfuscator and the Delegaterestore tools to do half the work of deobbing the crypto assembly, howver here soon i will be attempting to buil dmy own delegate remover.
The Deobber.s file is where the string, constants, junk code remover, and windows forms control renamer lie. 
All the windows forms control renamer does is during the final step in patching, the patcher checks the type to see if its a Winform, if it is, then it locates the InitializeComponent method and renames if needed.
Once InitializeComponent is located, an instance of the form is created in memory, then all the controls are found and the value of their Name property is used (if set) to rename the field that contains the control.
