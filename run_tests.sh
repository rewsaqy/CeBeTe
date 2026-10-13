#!/bin/bash
/opt/unity/Editor/Unity -runTests -projectPath $(pwd) -testResults $(pwd)/test_results.xml -testPlatform PlayMode -batchmode -nographics
