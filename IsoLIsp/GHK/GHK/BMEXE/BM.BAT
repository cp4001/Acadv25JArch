@echo off
rename %1.txt %1. >nul
for %%f in (%1) do call cal %%f >nul
