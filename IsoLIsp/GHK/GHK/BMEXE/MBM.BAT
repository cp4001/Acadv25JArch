@echo off
rename %1.DWG %1. >nul
for %%f in (%1) do call mbm1 %%f >nul
