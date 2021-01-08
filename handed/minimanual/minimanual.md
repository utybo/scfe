---
title: "SCFE: Installation & Operating Manual"
author:
- Matthieu Stombellini
- Mathieu Rivier
- François Soulier
- Rakhmatullo Rashidov
toc: true
toc-depth: 2
numbersections: true
fontsize: 12pt
papersize: a4
documentclass: article
geometry: margin=3.5cm
include-before: \newpage
include-after-toc: \newpage
logo: slablogo.png
icon: scfelogo.png
graphics: true
---

# Introduction

This manual provides a short guide on how to install and use SCFE. It is divided
in two sections: one for installing SCFE and one for the basic instructions to
get started.

# Installation and removal

## Installing

Installing SCFE is done like any other application on Windows.

If you do not have a copy of the installer, you can download one from
salamanders.dev under the Downloads tab. The yellow button should give you the
version for your operating system, but you can also scroll down to download any
version.

Double click on the EXE file. Should a SmartScreen security prompt appear, click
on "Additional information", then choose the button that just appeared to run
the installer. Another prompt should appear asking for elevated permissions,
click on Yes.

Then, the welcome page of the installer should appear. Press Next. You will then
be able to modify the path in which SCFE will be installed, although the default
directory should work just fine. Once this is done, click on the Install button.

SCFE will now be installed on your computer. This could take a few minutes
depending on your computer's speed. Once the installation is done, click on the
Finish button to close the installer.

## Uninstalling

Launching the uninstaller software can be done through three ways:

* Go in the Start Menu, go to the programs view, look for the SCFE folder, click
  on it, and click on "Uninstall SCFE"
* Go to the Windows Settings app, choose Applications, scroll down the list
  until you see "SCFE", click on it and click on the Uninstall button
* Go to the Control Panel, click on Uninstall a program, scroll down the list
  and click on SCFE, then choose Uninstall.

When launching the installer, you should see a security prompt: press Yes. Then,
simply press Next and then Uninstall in the removal software. Once the
uninstaller is done removing SCFE, press Finish to close it.


# Using SCFE

Documentation on more advanced features is available in the Documentation
section of the salamanders.dev website.

Launch SCFE from the Start Menu. You will be greeted by a screen with a few
sections. At the top, you have one line with, on the left, the name of the
directory you are in, and on the right the number of files being shown. This
number has a star if the view is being filtered.

Note that if you are in your user directory, the `~` symbol is shown instead of
the path to your user directory.

In the middle, you have your files. You can go up and down using the arrow keys
(you can also use J and K in NAV mode), you can go to the parent folder with the
left arrow  key (you can also use H in NAV mode), open a file or folder using
the right arrow key or the Enter key.

You can see all of the actions you can perform on a file by pressing the O key
(like in "Options"). You can see all of the actions for the folder you are in
with the Shift+O shortcut.

At the bottom, you will find on the left the word "NAV", "SEA" or "COM", which
correspond to the current mode the application is in, and next to it a text
which will change when you use the app and tells you what is happening. At the
very bottom, there is an input box. Should an action require input from you
(e.g. renaming a file), you will be asked to enter the information at the
bottom of the screen. The Search mode also uses the input box to show you the
current search terms. You can cancel an action or a search with the Escape key.

The mode system is the core of SCFE and allows you to switch back and forth
between two ways of using the application:

* NAV mode, the default. It allows you to perform actions with very little
  keystrokes and navigate with the arrow keys.
* SEA mode, for searching. The shortcuts are the same as NAV mode but you will
  need to press the Ctrl key in addition to the other keys. Typing in letters
  will search for files containing the search terms. Pressing Enter while in
  the input box in Search mode will directly open the matched file or folder if
  only one element corresponds to the search term. If more than one element
  were found, pressing Enter will allow you to choose between them.

For more information on more advanced features, you can have a look at the
documentation on the website, or use the O and Shift+O shortcuts to see all of
the actions that are available.

Press the Escape key twice to exit SCFE. A third Escape key press might be
necessary if you were in the input box.
