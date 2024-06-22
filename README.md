Phext Notepad
-------------
phext notepad provides a reference implementation of 11 dimensional plain hypertext in C#. You can use this editor to test your tools and verify that your phext encoding is accurate. If you find any bugs in this implementation, please file a bug report!

Phext-Based File Formats
------------------------
Plain hypertext (phext) provides modern systems with a large text space. This format is suitable for serializing many datasets, as it provides 11 dimensions of free-form text. Traditional editors only explore 2-dimensional text (columns and lines). Operating systems provide access to files and folders - providing access to a 4-dimensional text space.

With 11 dimensions, we can efficiently refer to most of the information available on the Internet in 2022. Given that a vast majority of our capacity is used for video, it seems reasonable to assume that we will not need more than this for quite a long time.

A single phext file could easily require 1 yottabyte of storage, or as little as 100 bytes.

Typical Source File
-------------------
A reasonable size for a C++ source file is perhaps 10 KB. With an average line length of 30 characters, you'd get 333 lines of source. For the x and n dimensions, we expect roughly N=100.

Scaling Text
------------
As we add dimensions, we can encode more complex things. Below are some examples. Assume you have access to a computing node with 100 GB of storage (i.e. a 2020 Smartphone).

* 1: concepts (100 bytes) => 100 billion
* 2: files (10 KB) => 10 million
* 3: components (1 MB) => 100,000
* 4: programs (100 MB) => 1,000
* 5: systems (10 GB) => 10
* 6: networks (1 TB) => You are here
* 7: services (100 TB) => 5x HDDs or 50x SSDs in 2022
* 8: social media (10 PB) => 500 HDDs (Wikipedia)
* 9: cloud storage (1 EB) => 50,000 HDDs (Netflix)
* 10: cloud provider (100 EB) => 5,000,000 HDDs (Google)
* 11: storage provider (1 YB) => 500,000,000 HDDs (Seagate)

Formatting Conventions
----------------------
Megatext files support escape sequences for compatibility with traditional editors.

Special Dimensions
------------------
The first two dimensions are special. The comment plane (c) exists outside of the normal 11-dimensional text space. It can be used to annotate any text within the document (in place). The data dimension ignores the text space entirely, forming a one-dimensional byte array following the first occurrence of a \z byte. This can be used to append binary data to the end of a megatext file.

* c = Comment (annotations, notes, footnotes)
* z = Data Dimension (binary blobs)

Text Dimensions
---------------

* \x = Column
* \n = Line
* \p = Scroll
* \g = Section
* \s = Chapter
* \y = Book
* \h = Volume
* \e = Collection
* \w = Series
* \i = Shelf
* \m = Library

Ruler
-----
Library: 1,  Shelf: 1,  Series: 1,  Collection: 1,  Volume: 1,  Book: 1,  Chapter: 1,  Section: 1,  Scroll: 1,  Line: 1,  Column: 1

Historic Control Codes
----------------------
Conforming editors must implement MORE COWBELL!

    000   0     00    NUL '\0'                    
    001   1     01    SOH (start of heading)      
    002   2     02    STX (start of text)         
    003   3     03    ETX (end of text)           
    004   4     04    EOT (end of transmission)   
    005   5     05    ENQ (enquiry)               
    006   6     06    ACK (acknowledge)           
    007   7     07    BEL '\a' (bell)             
    010   8     08    BS  '\b' (backspace)        
    011   9     09    HT  '\t' (horizontal tab)   
    012   10    0A    LF  '\n' (new line)         
    013   11    0B    VT  '\v' (vertical tab)     
    014   12    0C    FF  '\f' (form feed)        
    015   13    0D    CR  '\r' (carriage ret)     
    016   14    0E    SO  (shift out)             
    017   15    0F    SI  (shift in)              
    020   16    10    DLE (data link escape)      
    021   17    11    DC1 (device control 1)      
    022   18    12    DC2 (device control 2)      
    023   19    13    DC3 (device control 3)      
    024   20    14    DC4 (device control 4)      
    025   21    15    NAK (negative ack.)         
    026   22    16    SYN (synchronous idle)      
    027   23    17    ETB (end of trans. blk)     
    030   24    18    CAN (cancel)                
    031   25    19    EM  (end of medium)         
    032   26    1A    SUB (substitute)            
    033   27    1B    ESC (escape)                
    034   28    1C    FS  (file separator)        
    035   29    1D    GS  (group separator)       
    036   30    1E    RS  (record separator)      
    037   31    1F    US  (unit separator)  

Remapped Dimension Controls
---------------------------
01: \m Library Break
02: \z Binary Start
05: \q Coordinates Start
06: \k Coordinates End
0A: \n Line Break
0B: \v Vertical Tab
0D: \r Carriage Return
0E: \c Start Comment
0F: \d End Comment
17: \p Scroll Break
18: \g Section Break
19: \s Chapter Break
1A: \y Book Break
1C: \h Volume Break
1D: \e Collection Break
1E: \w Series Break
1F: \i Shelf Break

Megatext Initialization Information
-----------------------------------
A .tii file describes the control code remapping available in your megatext files. The default .tii for conforming text editors can be tweaked to resolve compatibility issues with your system. Dimension controls are specified. If no change is specified, then historic behaviors for that character are assumed.

{
  "01": { "\\m": "++M;GWVUTSRZXY=0" },
  "02": { "\\z": "D=1" },
  "03": { "\\l": "EndOfLine" },
  "04": { "\\eof": "EndOfFile" },
  "05": { "\\j": "JumpStart" },
  "06": { "\\u": "JumpEnd" },
  "0A": { "\\n": "++Y;X=0" },
  "0B": { "\\v": "++Y" },
  "0E": { "\\c": "C=1" },
  "0F": { "\\d": C=0" },
  "17": { "\\p": "++Z;XY=0" },
  "18": { "\\g": "++R;ZXY=0" },
  "19": { "\\s": "++S;RZXY=0" },
  "1A": { "\\o": "++T;SRZXY=0" },
  "1C": { "\\h": "++U;TSRZXY=0" },
  "1D": { "\\e": "++V;UTSRZXY=0" },
  "1E": { "\\w": "++W;VUTSRZXY=0" },
  "1F": { "\\i": "++I;WVUTSRZXY=0" }
}

Examples
--------

Let's say that we want to encode all of the information for a large-scale software project into one file...

Assume 60 MB of source. We can allocate page sizes of 4 KB (80x50), giving us 15,000 files to work with. We want to organize those into 12 sub-systems, each with 50 modules composed of 25 files each.

We'll use these breaks to organize things: \n, \p, \g, and \s.

Sub-system 1, Module 1, File 1 \p\n
...
Sub-system 1, Module 1, File 25 \g\n
Sub-system 1, Module 2, File 1 \p\n
Sub-system 1, Module 2, File 25 \g\n
Sub-system 1, Module 50, File 25 \s\n
Sub-system 2, Module 1, File 1 \p\n
...
Sub-system 12, Module 50, File 24 \p\n
Sub-system 12, Module 50, File 25 \eof
