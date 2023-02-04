This utility tries to update all mods in Witcher 3 directory.

To use this:
  - have modded Witcher 3 and be inpatient enough to wait for mod authors to update their mods
  - install KDiff3 (TODO: bundle)
  - clone Witcher 3 scripts repository (https://github.com/SnakeBite94/Witcher3-Scripts)(TODO: bundle git and clone)
    Hopefully there will be a commit with current version - if not, contact me on Nexus (SnakeBite94)
  - configure this app using config.json
  - (optional) install Witcher 3 Script Merger using Vortex

What this does, step by step:
1) The scripts repository contains versions of Witcher3 script files for every game version
   Using this, lsit all files affected by recent patch
2) For all affected files, find all corresponding mods in Mods folder containing this file
3) For every such file, find a current vanilla version (for current patch) in content folder
4) Call KDiff, use        
    - affected file from previous version (from scripts repository) as "base".
    - current vanilla file from content folder as "theirs"
    - modded file as "ours"
5) Merge can be automatic, only showing KDiff window if merge conflicts arise - happens if:
    - Mod fixed something and new patch fixed the same line (such as Brothers In Arms mod)
    - Official patch changed some lines
    - Mod was not properly updated and contains different whitespaces
6) Merge writes into modded file
7) Run WitcherScriptMerger - use the tool to delete merges.
   New merges should now contain no conflicts caused by the patch. Conflicts between mods still can happen though.


Updated mods may directly be zipped and uploaded to Nexus ;)
