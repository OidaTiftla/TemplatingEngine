# LatexScriptWrapper
Enables you to write C#-Code in your .tex-files and compile them in your .NET-Application

If you have any improvements, ideas, ... please let me know ;-)

# Getting Started
1. Clone this repository [https://github.com/OidaTiftla/LatexScriptWrapper.git](https://github.com/OidaTiftla/LatexScriptWrapper.git)
2. Download and install some LaTeX compiler. For example [MiKTeX](http://miktex.org/download) or the [portable version](http://miktex.org/portable)
3. edit the command in [Configuration.xml](https://github.com/OidaTiftla/LatexScriptWrapper/blob/master/LatexScriptWrapper/Config/Configuration.xml)
    * the Configuration.xml is searched first in the directory of the .exe and then in the subdirectory Config/Configuration.xml
    * you can also specify the configuration when creating a new TexWrapper object

# Classes
The main classes are CSScriptCompiler, TexWrapper and CSTexReportGenerator
## CSScriptCompiler
Compiles the .tex-file with the embedded C#-Code in it to an IScript. This script you can run and get a string back, that contains the processed input-file. The run-method acceptes a dynamic object. With this object you can pass values to the script.
## TexWrapper
It holds the configuration for the LaTeX-compiler and runs it to create the .pdf-file
## CSTexReportGenerator
Handles some stuff around the two classes CSScriptCompiler and TexWrapper, to make world a bit easier.
While running, it creates a .texcs-file, that contains the output from the CSScriptCompiler. This goes into the LaTeX-compiler (TexWrapper).

# Escape-Sequences
There are some standard escape-sequences CSTexReportGenerator creates for you. Offcoures you can add your own ones ;)

		%## Here you can put your C#-Code ##%
		\verb|## Here you can put your C#-Code ##|
		\verb$## Here you can put your C#-Code ##$
		\begin{comment}## Here you can put your C#-Code ##\end{comment}

# Example
## Sample LaTeX-file
When feeding this source file

		\documentclass{article}

		% switching between demo modus and c#
		\newcommand{\cs}[2]{#2}
		%## print @"\renewcommand{\cs}[2]{#1}"; ##%

		\begin{document}
		\textbf{}\hfill{\Huge Test} \hfill \cs{\verb|## print DateTime.Now.ToShortDateString(); ##|}{14.09.2015}

		%## print Tex.Escape("äöü-_'!\"§$%&/()=?^<>{[]}\\~"); ##%
		\cs{{## print o.Value; ##}}{Test}

		\end{document}

It will create

		\documentclass{article}

		% switching between demo modus and c#
		\newcommand{\cs}[2]{#2}
		\renewcommand{\cs}[2]{#1}

		\begin{document}
		\textbf{}\hfill{\Huge Test} \hfill \cs{14/09/2015}{14.09.2015}

		äöü-\_'!"'\S\$\%\&/()=?\hat{\text{\ }}{\textless}{\textgreater}\{[]\}{\texttt{\char`\\}{\textasciitilde}
		\cs{25}{Test}

		\end{document}

What goes into the LaTeX-compiler.

## Sample C#-code
Here is a sample script:

		// Create the RepotGenerator
		// Here you can pass a Configuration, otherwise
		// CSTexReportGenerator searches Configuration.xml
		// in the current directory and if it is not there
		// he searches it in ./Config/
		var generator = new CSTexReportGenerator();

		// Create an object, that you can use in your LaTeX
		var o = new
		{
			Value = 25,
			x = false,
			y = true,
			z = false,
			Headers = new string[] { "First", "Second", "Third", "Fourth" },
			Entries = new[] {
				new[] { "11", "12", "13", "14" },
				new[] { "21", "22", "23", "24" },
				new[] { "31", "32", "33", "34" },
				new[] { "41", "42", "43", "44" },
				new[] { "51", "52", "53", "54" },
				new[] { "61", "62", "63", "64" },
			},
		};

		// Create the PDF
		var src = new FileInfo("TestFile.tex");
		var dest = new FileInfo("TestFile.pdf");
		generator.Create(src, o, dest);

		// Show the PDF
		Process.Start(dest.FullName);
