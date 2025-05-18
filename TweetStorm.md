Introduction
------------                                                               
Traditional text files are great. You can store an entire page of text with
zero overhead. A typical page is 2-4 KB in size (80 x 50 = 4000). There's
no extra formatting or markup. Text is the OG WYSIWYG format.

Let's review what text files are from first-principles:
* Text = a serialized (1-dimensional) stream of characters
* Page = a 2-dimensional array of _lines_

What might text in higher dimensions look like? How would we make sense of
it? Why does it even matter? These are questions you should ponder as you
study what Phext can help you do.

Side note: The PC industry settled on variable-width text because most lines
are blank. You can think of early-return line breaks as a form of very cheap
text compression. This document is 4,551 bytes and 95 lines - resulting in
an average line length of 48 bytes. But the longest lines are 80 characters
(counting new lines). So without line breaks we would need 7,600 bytes. We've
achieved a 40% compression ratio simply by using line breaks.

This becomes especially important for higher-dimensional datasets in Phext.

History Lesson
--------------
TL;DR: 2 MIPS per KB

Early PCs (1976 - 1994) came with floppy disks that stored very little data
(80 KB to 1,440 KB). Using floppy disks was an exercise in patience: they
were SLOW. Seek times were often 200 ms, which significantly lowers the
average transfer rate from an already paltry 100 KB/sec.

An anecdotal example:
 * Filling a 1.2 MB 5.25" Floppy might take 12 seconds to 2 minutes

See: https://www.sciencedirect.com/topics/engineering/floppy-disk

The fastest CPU from this era is the 100 MHz Pentium, released on March 22,
1993. It featured a single 32-bit core with 3.2 million transistors. Power
usage was only 10 watts. It provided 188 million instructions per second.

This was the absolute fastest CPU any consumer had access to prior to 1994.
It was orders of magnitude faster than the CPUs of the 1980s. The
8086 (3 MHz) could execute 0.33 MIPS. The 80286 (6-12 MHz) hit 1.2-2.6 MIPS,
and the venerable 386 (16-33 MHz) could execute 5-11 MIPS.

See: https://www.eeeguide.com/features-of-80186-80286-80386-and-80486-microprocessor/
See: https://lowendmac.com/2014/cpus-intel-80286/

A modern i9-9900K achieves 412,090 MIPS in an 8-core configuration running
at 4.7 GHz. This is equivalent to a 1994-era supercomputer with 2,200
Pentium CPUs. Except it only draws 500 watts instead of 200 kilowatts. If
your cluster was built out of 386 boxes, you would need 37,000 CPUs. Using
a 286, the number grows to 150,000. And if we go back to the XT/AT days, you
would need 1.25 million computers.

Let's put that into context for pages of text. Since CPUs were very slow,
text was usually not compressed beyond the line break optimization noted
above. So a floppy disk might be able to load 50 pages of text per second,
once the drive head was moved to the correct sector. If those pages were
scattered across the disk, then throughput drops to something more like 5
pages per second.

Now, try convincing someone to work with higher-dimensional datasets
when their disk subsystem can only transfer 5-50 pages of text per
second. It would be pointless - a few page breaks might be all you need.

And thus, Word Processors were born. If we consider that Word Perfect for
the 386 fully solved desktop publishing, then we can estimate that text
editors require about 2 MIPS per KB. If we weren't limited by our I/O
interfaces, a modern i9 should be able to push about 200 MB/sec of text.

The Internet Era
----------------
In 1995, the Internet started to scale out very rapidly. At the same time,
computers became fast enough for games, video, and 3D graphics. Very
quickly, our systems went from being text machines to entertainment devices.
Along the way, we forgot why our text abstractions even existed: we just
took them for granted and did the best we could.

The AGI Era
-----------
Phext is designed for 2040 and beyond. It is assumed that humanity will
be using systems that routinely store petabytes of data and that we will
have brain interfaces which allow us to interact with our computers in
hyperspace. Computer screens will seem antiquated by then.

2022 - 2040
-----------
A modern PCIe 5.0 SSD is FAST. For random 4KB writes, you can achieve
transfer rates of 200 MB/sec (curiously, this is the same i9 limit above).
For large file sequential writes, the rate climbs to 10+ GB/sec. SSD Latency
("seek time") is 0.2 ms. This means that all point-in-time text-based
datasets are now trivial to write to disk. A 32-bit dataset is at most 4 GB,
which can be saved to disk in about 400 ms - IF you don't have it scattered
across a ton of files. Operating system overhead is now the I/O bottleneck.

Consider the system call overhead for a simple task of working with 60 MB
of source files for a medium-sized software project. If there are 2,000
files, then the average file is 31 KB and fits into 8 pages. But in order
to refactor this source tree, you will need to issue 2,000 context switches
between your application and the OS. You'll also have to maintain 2,000
separate file handles. Windows allows you to have up to 16 million file
handles per process. This implies an effective dataset size limit of about 500 GB, as
60 MB / 2000 files x 16 million files = 468 GB.

At 200 MB/sec, it would take us 40 minutes to work with this information
using our existing abstractions. But what if we could hit 100 GB/sec? Now it
becomes feasible to work with this dataset in 4 or 5 seconds.

See: https://learn.microsoft.com/en-us/archive/blogs/markrussinovich/pushing-the-limits-of-windows-handles

Going back to our floppy disk scenario...a modern SSD has the capacity of
about 3 million floppy disks and the transfer rate of about 100,000 drives.
CPUs are significantly faster as well, so compression on-the-fly is feasible
- boosting effective transfer rates for text by 10X. A modern disk can thus
write 100 GB/sec of compressed text.

This gives us access to a text space of 50 million pages. If we break this
text space up into manageable chunks, we will need a space that is 100 x 100
x 100 x 50 characters. This corresponds to the first 4 dimensions of Phext
(Columns, Lines, Scrolls, and Sections).

But Phext doesn't stop there. I intend for this format to be the text
format for the ASI age. So I turned the dial up to 11. This should give us
enough flexibility to manage very complex interactions with our computers.

File Format Design
------------------
Phexts are meant to be plain text extended to 11 dimensions.

Phext repurposes some historic ASCII control codes (0x01 - 0x1F) to provide
users with up to 11 dimensions of free-form text. All other characters not
listed here are standard UTF-8 text.

Scroll => 3D, Section => 4D,  Chapter    => 5D
Book   => 6D, Volume  => 7D,  Collection => 8D
Series => 9D, Shelf   => 10D, Library    => 11D
