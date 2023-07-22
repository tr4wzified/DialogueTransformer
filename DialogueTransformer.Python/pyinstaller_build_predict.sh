#!/bin/bash

# Run using Git Bash on Windows. Python 3.10, pyinstall and SimpleT5, pytorch and transformers are required.
# Yes I wrote a short bash shell script and not a bat or PS script, because fuck Windows command syntax :P

echo '[DialogueTransformer PredictBuilder] Compiling Python prediction file into standalone executable...'
pyinstaller DialoguePredictor.py
echo '[DialogueTransformer PredictBuilder] Copying compiled directory with LM to patcher internal data...'
cp -r dist/DialoguePredictor ../DialogueTransformer.Patcher/InternalData
echo '[DialogueTransformer PredictBuilder] Deleting build and dist folders from this directory...'
rm -r build
rm -r dist
echo '[DialogueTransformer PredictBuilder] Done!'
