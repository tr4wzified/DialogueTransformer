#!/bin/bash

# Run using Git Bash on Windows. Python 3.10, pyinstall and SimpleT5, pytorch and transformers are required.
# Yes I wrote a short bash shell script and not a bat or PS script, because fuck Windows command syntax :P

echo '[DialogueInferencingClientBuilder] Compiling Python prediction file into standalone executable...'
pyinstaller DialogueInferencingClient.py --copy-metadata tqdm --copy-metadata regex --copy-metadata sacremoses --copy-metadata requests --copy-metadata packaging --copy-metadata filelock --copy-metadata numpy --copy-metadata tokenizers --copy-metadata rich
echo '[DialogueInferencingClientBuilder] Copying compiled directory with LM to patcher internal data...'
cp -r dist/DialogueInferencingClient ../DialogueTransformer.Patcher/InternalData
echo '[DialogueInferencingClientBuilder] Deleting build and dist folders from this directory...'
rm -r build
rm -r dist
echo '[DialogueInferencingClientBuilder] Done!'
