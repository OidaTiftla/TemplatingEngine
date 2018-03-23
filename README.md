# Templating Engine
Enables you to generate files from templates with C#-Code inside.

If you have any improvements, ideas, ... please let me know ;-)

## Pre-requisites
1. Download and install some LaTeX compiler. For example [MiKTeX](http://miktex.org/download) or the [portable version](http://miktex.org/portable)
2. In your C# project edit the command in `Configuration.xml` to match to your LaTeX compiler installation (by default `pdflatex` should work fine)
    * The Configuration.xml is searched first in the directory of the .exe and then in the subdirectory `Config/Configuration.xml`
    * You can also specify the configuration when creating a new `TexWrapper` or `LatexEngine` object

## Usage

ToDo

## Using the LatexEngine

### Escape-Sequences

There are some standard escape-sequences CSTexReportGenerator creates for you. Offcoures you can add your own ones ;)

```tex
%## Here you can put your C#-Code ##%
\verb|## Here you can put your C#-Code ##|
\verb$## Here you can put your C#-Code ##$
\begin{comment}## Here you can put your C#-Code ##\end{comment}
```

### Example

#### Sample LaTeX-file

When feeding this source file

```tex
\documentclass{article}

% switching between demo modus and c#
\newcommand{\cs}[2]{#2}
%## print @"\renewcommand{\cs}[2]{#1}"; ##%

\begin{document}
\textbf{}\hfill{\Huge Test} \hfill \cs{\verb|## print DateTime.Now.ToShortDateString(); ##|}{14.09.2015}

%## print Tex.Escape("äöü-_'!\"§$%&/()=?^<>{[]}\\~"); ##%
\cs{{## print o.Value; ##}}{Test}

\end{document}
```

It will create

```tex
\documentclass{article}

% switching between demo modus and c#
\newcommand{\cs}[2]{#2}
\renewcommand{\cs}[2]{#1}

\begin{document}
\textbf{}\hfill{\Huge Test} \hfill \cs{14/09/2015}{14.09.2015}

äöü-\_'!"'\S\$\%\&/()=?\hat{\text{\ }}{\textless}{\textgreater}\{[]\}{\texttt{\char`\\}{\textasciitilde}
\cs{25}{Test}

\end{document}
```

What goes into the LaTeX-compiler and you get your PDF-file.

#### Sample C#-code

Here is a sample script:

```csharp
// Create the LatexEngine
// Here you can pass a Configuration, otherwise
// LatexEngine searches Configuration.xml
// in the current directory and if it is not there
// he searches it in ./Config/
var engine = new LatexEngine();

// Create an object, that you can use in your LaTeX
var o = new {
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
using (var srcStream = src.OpenRead())
using (var destStream = dest.OpenWrite())
    engine.Generate(srcStream, o, destStream);

// Show the PDF
Process.Start(dest.FullName);
```

## Class model

The main classes are `CSharpEngine`, `LatexEngine`, `RazorEngine` and `T4Engine`.
`LatexEngine` uses `TexWrapper` to abstract the latex generation part into a separate class.

### CSharpEngine

Compiles and executes templates with embedded C#-Code in it.
You can pass values to the template through the context parameters.

### LatexEngine

Compiles and executes `.tex`-files with embedded C#-Code in it.
You can pass values to the template through the context parameters.
It uses `CSharpEngine` for compiling and executing a template.
While running, it creates a `.texcs`-file, that contains the output from the `CSharpEngine`.
This goes into the LaTeX-compiler (`TexWrapper`).

After generation you get an pdf file stream through the StreamWriter object.

#### TexWrapper

It holds the configuration for the LaTeX-compiler and runs it to create the `.pdf`-file

## Contributing

Pull requests for new features, bug fixes, and suggestions are welcome!

## License

[Apache License 2.0](LICENSE)
