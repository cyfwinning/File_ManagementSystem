@echo off
chcp 65001 >nul
echo ============================================
echo   企业智能知识库 - 一键启动
echo   浙江南芯半导体
echo ============================================
echo.

echo [1/2] 启动后端 API 服务 (端口 5000)...
start "KnowledgeBase API" cmd /c "cd /d %~dp0src\KnowledgeBase.WebApi && dotnet run --urls=http://localhost:5000"

echo [2/2] 启动前端开发服务 (端口 3000)...
start "KnowledgeBase Web" cmd /c "cd /d %~dp0frontend\knowledge-base-web && npm run dev"

echo.
echo 服务启动中，请等待...
echo   后端 API:  http://localhost:5000/swagger
echo   前端页面:  http://localhost:3000
echo   默认账号:  admin / admin123
echo.
echo 按任意键退出此窗口（服务将继续运行）
pause >nul
