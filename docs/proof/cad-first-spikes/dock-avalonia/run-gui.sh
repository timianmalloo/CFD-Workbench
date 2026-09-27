#!/bin/zsh
cd "$(dirname "$0")/DockProbe"
MODE=${1:-native}
dotnet bin/Debug/net10.0/DockProbe.dll $MODE > gui-$MODE.txt 2>&1 &
PID=$!
sleep 7
echo "pid=$PID"
osascript -e "tell application \"System Events\" to get name of windows of (first process whose unix id is $PID)" 2>&1
echo "osascript exit=$?"
osascript -e "tell application \"System Events\" to count windows of (first process whose unix id is $PID)" 2>&1
wait $PID
echo "app exit=$?"
cat gui-$MODE.txt
