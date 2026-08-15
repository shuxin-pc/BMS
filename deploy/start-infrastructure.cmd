@echo off
chcp 65001 >nul
echo 启动MES系统基础设施...
docker-compose -f docker-compose.infrastructure.yml up -d
echo 基础设施启动完成！
echo.
echo 服务访问地址：
echo PostgreSQL: localhost:5432
echo Redis: localhost:6379
echo Consul UI: http://localhost:8500
echo RabbitMQ Management: http://localhost:15672 (用户名/密码: mes/mes@2026)
echo Kafka: localhost:9092
echo Kafka UI: http://localhost:8080 (Kafka可视化管理界面)
echo MinIO: localhost:9000 (本机原生部署，不由本脚本启动，见 docs/部署文档/MinIO部署说明.md)
echo.
echo 查看服务状态: docker-compose -f docker-compose.infrastructure.yml ps
echo 停止服务: docker-compose -f docker-compose.infrastructure.yml down
echo 查看日志: docker-compose -f docker-compose.infrastructure.yml logs -f [服务名]
pause
