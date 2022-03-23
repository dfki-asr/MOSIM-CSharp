@echo off

REM SPDX-License-Identifier: MIT
REM The content of this file has been developed in the context of the MOSIM research project.
REM Original author(s): Janis Sprenger, Bhuvaneshwaran Ilanthirayan, Klaus Fischer

REM This is a deploy script to auto-generate the components of the MOSIM-CSharp projects and move them to a new environment folder. 

ECHO " ------------------------------------------ " 
ECHO "       _   __ ___                  _        "
ECHO " |\/| / \ (_   |  |\/|     __     /  -|-|-  "
ECHO " |  | \_/ __) _|_ |  |            \_ -|-|-  "
ECHO "                                            "                                          
ECHO " ------------------------------------------ " 
ECHO.

SET VERBOSE=0

call :CheckEnv

call :argparse %*

goto :eof

REM Method Section

:: argparse
:argparse
	if [%1]==[] (
		call :DisplayUsage
		exit /b 0
	)
	if "%1"=="-h" (
		call :DisplayUsage
		exit /b 0
	)
	if "%1"=="--help" ( 
		call :DisplayUsage
		exit /b 0
	)
	if "%1"=="\?" ( 
		call :DisplayUsage
		exit /b 0
	)
	
	FOR /F %%i IN ("%1") DO SET "MOSIM_HOME=%%~fi"
	
	if not exist %1 (
		call :FolderNotFound
		exit /b 1
	) else (
		FOR /F %%i IN ("%1") DO SET "MOSIM_HOME=%%~fi"
		
		echo Deploying to: %MOSIM_HOME%
		SET BUILDENV=%MOSIM_HOME%\Environment
		SET LIBRARYPATH=%MOSIM_HOME%\Libraries
		SET REPO=%~dp0\..\
		

		SHIFT
	)
	
	if "%1"=="-v" (
		ECHO Running in Verbose mode
		SET VERBOSE=1
		SHIFT
	)
	
	if [%1]==[] (
		call :DeployAll
		exit /b 0
	)

	:argparse_loop
	if not [%1]==[] (
		if "%1"=="-m" (
			if "%2"=="Adapter" (
				call :DeployAdapter 
			) else (
				if "%2"=="CoSimulator" (
					call :DeployCoSimulator
				) else ( 
					if "%2"=="Launcher" (
						call :DeployLauncher
					) else (
						if "%2"=="MMUs" (
							call :DeployMMUs 
						) else ( 
							if "%2"=="SkeletonAccess" ( 
								call :DeploySkeletonAccess 
							) else ( 
								if "%2"=="Retargeting" ( 
									call :DeployRetargetingService 
								) else ( 
									if "%2"=="PostureBlending" ( 
										call :DeployPostureBlendingService 
									) else ( 
									if "%2"=="CoordinateSystem" ( 
										call :DeployCoordinateSystemMapper 
									)
								) 
								) 
							) 
						)
					)
				)
			)
			SHIFT
		) else ( 
			if "%1"=="-a" (
				call :DeployAll
			)
		)
		SHIFT
		goto :argparse_loop
	)
exit /b 0

::DisplayUsage
:DisplayUsage
	echo Usage
exit /b 0

::FolderNotFound
:FolderNotFound
	echo Folder Not Found
exit /b 0

::DeployAdapter
:DeployAdapter
	call :MSBUILD "%REPO%\Core\Adapter\Adapter.sln" MMIAdapterCSharp\bin Adapters\CSharpAdapter\
exit /b 0

::DeployCoSimulator
:DeployCoSimulator 
	call :MSBUILD "%REPO%\Core\CoSimulator\MMICoSimulator.sln" CoSimulationStandalone\bin Services\CoSimulationStandalone\
exit /b 0

::DeployLauncher
:DeployLauncher
	call :MSBUILD "%REPO%\Core\Launcher\MMILauncher.sln" MMILauncher\bin Launcher\
	COPY "%REPO%\Deployment\defaultSettings.json" "%BUILDENV%\Launcher\settings.json"

exit /b 0

::DeployMMUs 
:DeployMMUs 
echo Deploy MMUs
	call :DeployMethod %REPO%\MMUs MMUs\ build
	
	RMDIR /s/q %BUILDENV%\\MMUs\\CarryMMU
	RMDIR /s/q %BUILDENV%\\MMUs\\CarryMMUNested
	RMDIR /s/q %BUILDENV%\\MMUs\\CarryMMUSimple
	RMDIR /s/q %BUILDENV%\\MMUs\\MoveMMU
	RMDIR /s/q %BUILDENV%\\MMUs\\MoveMMUSimple
	RMDIR /s/q %BUILDENV%\\MMUs\\ReachMMU
	RMDIR /s/q %BUILDENV%\\MMUs\\ReleaseMMU
exit /b 0
	
::DeploySkeletonAccess
:DeploySkeletonAccess
	call :MSBUILD "%REPO%\Services\SkeletonAccessService\SkeletonAccessService.sln" SkeletonAccessService\bin Services\SkeletonAccessService
exit /b 0

::DeployRetargetingService
:DeployRetargetingService
	call :MSBUILD "%REPO%\Services\RetargetingService\RetargetingService.sln" RetargetingService\bin Services\RetargetingService\
exit /b 0

::DeployPostureBlendingService
:DeployPostureBlendingService
	call :MSBUILD "%REPO%\Services\PostureBlendingService\PostureBlendingService.sln" PostureBlendingService\bin Services\PostureBlendingService\
exit /b 0

:DeployCoordinateSystemMapper
	call :MSBUILD "%REPO%\Services\CoordinateSystemMapper\CoordinateSystemMapper.sln" CoordinateSystemMapper\bin Services\CoordinateSystemMapper\
exit /b 0

::DeployAll
:DeployAll
	call :DeployAdapter
	call :DeployCoSimulator
	call :DeployLauncher
	call :DeployMMUs
	call :DeploySkeletonAccess
	call :DeployRetargetingService
	call :DeployPostureBlendingService
	call :DeployCoordinateSystemMapper
exit /b 0

::DeployMethod 
::  %1 path to component
::  %2 target path
::  %3 build path in component
:DeployMethod
  REM Build Adapters
  set back=%CD%
  
  if exist %1 (
	  cd %1
	  call :safeCall .\deploy.bat "There has been an error when deploying %1" %back%
	  cd %back%
	  if not [%2]==[] (
		  md ".\%BUILDENV%\%2"
		  echo  "%1\%3\*" "%BUILDENV%\%2\"
		  cmd /c xcopy /S/Y/Q "%1\%3\*" "%BUILDENV%\%2\"
		  if %ERRORLEVEL% NEQ 0 echo There has been an error during copy. 
		  REM if %ERRORLEVEL% NEQ 0 cd %MOSIM_HOME% && call :halt %ERRORLEVEL%
	  )
  ) else (
    ECHO -----------
	ECHO [31m Path %1 does not exist and thus will not be deployed.[0m
	ECHO -----------
  )
exit /b

::MSBUILD
:MSBUILD
  for /F "delims=" %%i in (%1) do set dirname="%%~dpi"
  for /F "delims=" %%i in (%1) do set filename="%%~nxi"
  
  set mode=Debug
  SETLOCAL EnableDelayedExpansion 
  
  set back=%CD%
  
  if exist %dirname% (
	cd %dirname%
	
	if %VERBOSE%==1 (
		"%MOSIM_MSBUILD%" %filename% -t:Build -p:Configuration=%mode% -flp:logfile=build.log
	) else (
		>deploy.log (
			"%MOSIM_MSBUILD%" %filename% -t:Build -p:Configuration=%mode% -flp:logfile=build.log
		)
	)
	REM If the build was sucessfull, copy all files to the respective build folders. 

	if !ERRORLEVEL! EQU 0 (
		if not [%2]==[] (
			if %VERBOSE%==1 (
				ECHO copy from ".\%2\%mode%\*" to "%BUILDENV%\%3\"
				cmd /c xcopy /S/Y/Q ".\%2\%mode%\*" "%BUILDENV%\%3\"
			) else (
				>>deploy.log (
					cmd /c xcopy /S/Y/Q ".\%2\%mode%\*" "%BUILDENV%\%3\"
				)
			)
		)
		if not [%4]==[] (
			if %VERBOSE%==1 (
				cmd /c xcopy /S/Y/Q ".\%4\%mode%\*" "%BUILDENV%\%5\"
			) else (
				>>deploy.log (
					cmd /c xcopy /S/Y/Q ".\%4\%mode%\*" "%BUILDENV%\%5\"
				)
			)
		)
		if not [%6]==[] (
			if %VERBOSE%==1 (
				cmd /c xcopy /S/Y/Q ".\%6\%mode%\*" "%BUILDENV%\%7\"
			) else (
				>>deploy.log (
					cmd /c xcopy /S/Y/Q ".\%6\%mode%\*" "%BUILDENV%\%7\"
				)
			)
		)
		ECHO [92mSuccessfully deployed %filename%. [0m
	) else (
		type deploy.log 
		ECHO [31mDeployment of %filename% failed. Please consider the build.log for more information.[0m 
		cd %back%
		call :halt %ERRORLEVEL%
	)
  ) else (
    ECHO -----------
	ECHO [31m Path %1 does not exist and thus will not be deployed.[0m
	ECHO -----------
  )
cd %back%
exit /b

:: Calls a method %1 and checks the error level. If %1 failed, text %2 will be reported. 
:safeCall
SET back=%3
call %1
if %ERRORLEVEL% NEQ 0 (
  ECHO [31m %~2 [0m
  cd %back%
  call :halt %ERRORLEVEL%
) else (
	exit /b
)

::Check Environment Variables
:CheckEnv
	IF NOT "%MOSIM_MSBUILD%"=="" (
		IF NOT EXIST "%MOSIM_MSBUILD%" (
			ECHO Please update your environment variable MOSIM_MSBUILD to point to Visual Studio MSBUILD.
			ECHO example: setx MOSIM_MSBUILD "C:\Program Files (x86)\Microsoft Visual Studio\2019\Professional\MSBuild\Current\Bin\MSBuild.exe"
			call :halt 1
		)
	) ELSE (
		ECHO Compilation requires Visual Studio. Please setup the variable MOSIM_MSBUILD to point to Visual Studio MSBUILD.
		ECHO example: setx MOSIM_MSBUILD "C:\Program Files (x86)\Microsoft Visual Studio\2019\Professional\MSBuild\Current\Bin\MSBuild.exe"
		call :halt 1
	)
exit /b 0


:: Sets the errorlevel and stops the batch immediately
:halt
call :__SetErrorLevel %1
call :__ErrorExit 2> nul
goto :eof

:__ErrorExit
rem Creates a syntax error, stops immediately
() 
goto :eof

:__SetErrorLevel
exit /b %time:~-2%
goto :eof



REM Nothing should folow after this. 