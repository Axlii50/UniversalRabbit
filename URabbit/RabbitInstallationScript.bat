@echo off
setlocal enabledelayedexpansion

REM ---- Sciezki i wersje do modyfikacji w razie potrzeby ----
set ERLANG_VERSION=25.3
set RABBITMQ_VERSION=4.1.3
set ERLANG_URL=https://erlang.org/download/otp_win64_%ERLANG_VERSION%.exe
set RABBITMQ_URL=https://github.com/rabbitmq/rabbitmq-server/releases/download/v%RABBITMQ_VERSION%/rabbitmq-server-%RABBITMQ_VERSION%.exe

REM ---- Sciezki instalacji ----
set ERLANG_HOME=C:\Program Files\Erlang OTP
set RABBITMQ_HOME=C:\Program Files\RabbitMQ Server\rabbitmq_server-%RABBITMQ_VERSION%

REM ---- Pobierz i zainstaluj Erlanga ----
echo [INFO] Pobieranie Erlang %ERLANG_VERSION%...
curl -L -o otp.exe %ERLANG_URL%
pause

echo [INFO] Instalacja Erlang...
start /wait otp.exe /S

REM ---- Dodaj ERLANG_HOME do zmiennych srodowiskowych ----
echo [INFO] Dodawanie ERLANG_HOME do PATH...
setx ERLANG_HOME "%ERLANG_HOME%\%ERLANG_VERSION%"
setx PATH "%PATH%;%ERLANG_HOME%\bin"

REM ---- Pobierz i zainstaluj RabbitMQ ----
echo [INFO] Pobieranie RabbitMQ %RABBITMQ_VERSION%...
curl -L -o rabbitmq.exe %RABBITMQ_URL%

echo [INFO] Instalacja RabbitMQ...
start /wait rabbitmq.exe /S

REM ---- Rejestruj RabbitMQ jako usluge ----
echo [INFO] Rejestracja RabbitMQ jako usługa Windows...
cd /d "%RABBITMQ_HOME%\sbin"
rabbitmq-service.bat install

REM ---- Uruchom usluge RabbitMQ ----
echo [INFO] Uruchamianie usługi RabbitMQ...
rabbitmq-service.bat start

REM ---- (Opcjonalnie) Wlacz panel webowy (Management Plugin) ----
echo [INFO] Włączanie pluginu Management...
rabbitmq-plugins.bat enable rabbitmq_management

echo.
echo [DONE] RabbitMQ zainstalowany i uruchomiony jako usluga!
echo [PANEL] http://localhost:15672
echo Login: guest
echo Haslo: guest

pause
