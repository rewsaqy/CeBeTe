#!/bin/bash
"C:\Program Files\Unity\Hub\Editor\6000.0.21f1\Editor\Unity.exe" -runTests -projectPath $(pwd) -testResults $(pwd)/test_results.xml -testPlatform PlayMode -batchmode -nographics
