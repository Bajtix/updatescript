# updateScript
A small C# program that can update/download anything following an install script. 

Check the Releases tab for the downloads.

## How to use?
1. In your app's root folder create a text file. It is recommended to call it .uscript, but this is not required.
2. Put your update code in the script. Scroll down to see the language reference.
3. Save the file.
4. If you want to update, call `updatescript.exe {path}`, with the path leading to the install script file. You can call this from the commandline, or your own launcher app. I'll try and make it a little easier to directly integrate into your code, but for now the .exe is the best way.
5. Test if it works.

It is recommended that you create your own launcher app, as this program does not automatically fetch the new updateScript file (maybe in the near future?). This program is not the best solution, but it should work if you need a quick solution. Feel free to use the sourcecode in your own project and to modify this one to your demand. (Giving credit is appreciated, but not required)

## How does it work?
When you call the command, the program executes the file line by line. If it has ran before, it checks if the updateScript contains a new version, comparing it to the `.usversion` file. The file is created after an uodate is finished. The program starts from the first version, which's number is bigger than the one stored in the file. It always runs the script to the end. Check the reference to see how the versions should be marked.

## updateScript Microlanguage Reference
The scripting language is very simple. The file starts with a header. Each line is a new command, with the first character being the command itself, and the rest arguments. Arguments are separated by `;` and they can be wrapped inside of `"`.
### Header
The header for now contains only the language version. It **has** to be the first line of the file. The correct header for the current program version is
```
updatescript v1.0
```

### Commands
| Prefix | Arguments    | Description                                                               | Example                                                                                                 | Use `"` ?                                                         |
|--------|--------------|---------------------------------------------------------------------------|---------------------------------------------------------------------------------------------------------|-------------------------------------------------------------------|
| #      | anything     | This is a comment. It is ignored by the app.                              | #A comment                                                                                              | Not required                                                      |
| +      | URL;PATH     | Downloads the file from URL to PATH                                       | `+"https://www.learningcontainer.com/wp-content/uploads/2020/05/sample-zip-file.zip";"path/sample.zip"` | Required*                                                         |
| -      | PATH         | Removes a file/directory at PATH                                          | `-"dir"` or `-"dir/somefile.txt"`                                                                       | Required*                                                         |
| /      | COMMAND      | Executes the COMMAND as a cmd command.                                    | `/type "zip\sample.txt"` this runs a batch command which prints file content.                           | No, but the arguments to the command which is passed may use them |
| ^      | ARCHIVE;PATH | Extracts files from zip archive ARCHIVE to directory PATH                 | `^"dir/sample.zip";"zip/"`                                                                              | Required*                                                         |
| ~      | SECONDS      | Stops the program for SECONDS seconds.                                    | `~10` waits 10 seconds                                                                                  | Do not use                                                        |
| '      | MSG          | Prints a debug message (MSG) in the console.                              | `'Version 0 installer`                                                                                  | Not required                                                      |
| @      | VERSION      | This is a version marker. It tells the program where new versions start.  | `@0`                                                                                                    | Do not use                                                        |
 
 
 *Technically, it should work without them but it is really not recommended and can cause issues.
 
 ## Example file
 ```
 updatescript v1.0

@0
#Print sth
'Version 0 installer

#Download file
+"https://www.learningcontainer.com/wp-content/uploads/2020/05/sample-zip-file.zip";"dir/sample.zip"

#Wait 10 secs
~10

#Unzip the test.zip file
^"dir/sample.zip";"zip/"

#Remove the file and directory
-"dir"

#Run a shell command
/type "zip\sample.txt" 
```
