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
The main classes are CSScriptCompiler, CSTexReportGenerator and TexWrapper
## CSScriptCompiler
Handles the embedded c#-Code in your .tex-file
## CSTexReportGenerator
Provides some extra handling, file handling, ...
While running, it creates a .texcs-file, that contains the code that comes from the CSScriptCompiler and goes into the LaTeX-compiler.
## TexWrapper
Runs the LaTeX-compiler to create the .pdf-file

# Example
When feeding this source file

		\documentclass{article}

		% switching between demo modus and c#
		\newcommand{\cs}[2]{#2}
		%## print @"\renewcommand{\cs}[2]{#1}"; ##%

		\begin{document}
		\textbf{}\hfill{\Huge Test} \hfill \cs{\verb|## print DateTime.Now.ToShortDateString(); ##|}{14.09.2015}

		%## print Tex.Escape("äöü-_'!\"§$%&/()=?^<>{[]}\\~"); ##%
		\cs{{## print 1; ##}}{Test}

		\end{document}

It will create

		\documentclass{article}

		% switching between demo modus and c#
		\newcommand{\cs}[2]{#2}
		\renewcommand{\cs}[2]{#1}

		\begin{document}
		\textbf{}\hfill{\Huge Test} \hfill \cs{14/09/2015}{14.09.2015}

		äöü-\_'!"'\S\$\%\&/()=?\hat{\text{\ }}{\textless}{\textgreater}\{[]\}{\texttt{\char`\\}{\textasciitilde}
		\cs{1}{Test}

		\end{document}

What goes into the LaTex-compiler.
