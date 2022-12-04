Welcome to the Terse
--------------------

Terse Notepad provides a reference implementation of multi-dimensional text using Lower ASCII dimension breaks. The format was created by Will Bickford, and is intended to assist humans with leaning into the Singularity.

Knowledge Trees
---------------
Terse files extend line breaks to additional dimensions. Everything that's great about text survives this transition. The .t format does not require any addressing or encoding rules. Scrolls provide you with infinite freedom to tinker and think. You can bundle up a set of scrolls and share them with others - while preserving the relational structure of your ideas.

High-Bandwidth I/O
------------------
For most of human history, we've been exchanging ideas at tragically slow rates. If you read very quickly, you might hit 3 bytes per second of compressed text. This is essentially a biological limit, but not one that will survive forever.

PCIe 5.0 SSDs are now providing us with 2-10 GB/sec data transfer speeds. For small files, these disks can sustain 200 MB/sec. Considering that text is highly-compressible, we now have access to disks that can transfer an effective 1 trillion words per minute.

  10 GB/sec x 10:1 compression ratio x 60sec/min
  ---------------------------------------------- = 1 trillion words/min
                  6 bytes/word

If you typed at 120 wpm for 15,000 years (without rest), you would only hit 950 billion words (5.7 GB). We now have an abundance of disk I/O at our disposal. It is time to re-think how we design our systems - the days of worrying about files measured in KB or MB are gone.

Machine Learning
----------------
Terse files are well-suited for machine learning, as they provide structure and context free of parsing concerns. This makes Terse files very robust. If a page becomes corrupted, a parser can easily skip past it. Perhaps the best way to think about Terse files is to consider them an anonymous collection of scrolls that someone shipped to you.
Terse Notepad Documentation

File
----
 * New: Create a Blank Terse File (Ctrl-N)
 * Open: Open an Existing Terse File (Ctrl-O)
 * Save: Write all scrolls to the current file (Ctrl-S)
 * Save As: Write all scrolls to a new file (Ctrl-Shift-S)
 * Exit: Leave the Terse?

Edit
----
 * Preferences: Open Terse.ini (Ctrl-,)

View
----
 * Dimension Report: Append a Text Summary of Your Terse Space (Ctrl-D)

Help
----
 * About: Display Program Version

Bugs

  * rich text paste is wonky
  * unit test coverage
Requests

  * scroll bars should do something
  * add multiple viewports
  * add dark mode
  * explore tabs
  * add zoomed UI  

Terse Release Notes

v0.0.9
------
  * 🌲 Tree view now updates in real-time

v0.0.8
------
  * 🌲 Tree view is now instant! \o/
    * New pages appear in the tree view once a line break occurs
    * Empty pages are cleared on save

v0.0.7
------
* Settings Version 3
  * Added ZoomFactor to preserve text resizing (separate from font size) - 1.0 = 100%
  * Added Recent File List (Sorted)
* User Requests
  * 🆕 Selecting the end of the row no longer selects the newline \o/

v0.0.6
------
* Editor Improvements
  * Editor State is now synchronized across sessions
  * Added Save As Menu Item
* Settings Version 2
  * Added Dimension Labels (Dimension1 through Dimension11)
  * Added WordWrap boolean - controls text box word wrapping
  * Added Font and FontSize
* Program Enhancements
  * Renamed executable to trs.exe
  * Renamed TODO.t to Terse.t
  * Added Release Notes to Terse.t
  * Added File Formats to Terse.t
  * Added Terse Notepad Documentation to Terse.t
  * Added "Welcome to the Terse" Summary
  * Embedded a copy of Terse.t on Build
  * Added License.md
* User Requests
  * 🦘 Removed the Jump button - pages load as you type now!
  * 🌲 Tree view updates on save and delete - more to come🎉
  * 🎄 Tree view selection is now single-click \o/

v0.0.5
------
* Switched to TODO.t
* First Version with .ini support
* Added Tree View
* Revised Coordinates: Library, Shelf, Series, Collection, Volume, Book, Chapter, Section, and Scroll
* Implemented Direct Page Jumps

v0.0.4
------
* Performance Tweaks and Bug fixes
* Limited Dimension Inputs to 4 digits

v0.0.3
------
* Added TODO.md
* Fixed Bugs with 5D Editing

v0.0.2
------
* Added Dimension Report to Assist with Sparse Page Views
* Added README.md
* Added Dimension Input Boxes

v0.0.1
------
* Proof of Concept with 5 of 11 dimensions
* Multiverse, Galaxy, World, Language, Branch, Volume, Set, Group, and Page

File Formats

This is a list of Terse-based file formats.

Extension  Status       Description
---------  ------       -----------
.t         Implemented  Reference File Format
.tweb      Proposed     HTML + Resources in One Byte Stream
.todb      Proposed     Text-Only Database
.tfl       Proposed     Text Flow Notes
.tjur      Proposed     Jurassic Park Style Viewer




































































