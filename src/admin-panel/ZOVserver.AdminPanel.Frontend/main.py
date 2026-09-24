import json
import logging
import secrets
import string
import time

import flet as ft
import grpc

from grpc_cp.proto import shop_service_pb2, user_service_pb2, user_service_pb2_grpc, messages_service_pb2, \
    messages_service_pb2_grpc, statistics_service_pb2_grpc, gifts_service_pb2_grpc, gifts_service_pb2, \
    statistics_service_pb2, shop_service_pb2_grpc

ADMIN_PANEL_BACKEND_ADDRESS = "localhost:50051" # !!!!!!!!!! (Now listening on: http://localhost:50051)

import os
os.environ["PROTOCOL_BUFFERS_PYTHON_IMPLEMENTATION"] = "python"

LOCALIZATIONS = {
    "ru": json.dumps({
        "app_title": "Админ-панель",
        "menu": {
            "title_main": "ADMIN",
            "title_sub": "DASHBOARD",
            "moderation": "Модерация",
            "messages": "Сообщения",
            "gifts": "Подарки",
            "shop": "Магазин",
            "statistics": "Статистика",
            "system_status": "СИСТЕМА",
            "system_online": "Онлайн",
            "system_version": "Версия ZOVserver V24"
        },
        "moderation": {
            "title": "Модерация",
            "subtitle": "Управление пользователями и их действиями",
            "live_status": "LIVE",
            "actions_card_title": "Действия с пользователем",
            "user_id_label": "ID пользователя",
            "user_id_hint": "#0289PYLQGRJCUV (1-16 символов)",
            "ban_button": "Бан",
            "lock_button": "Закрыть аккаунт",
            "unlock_unban_button": "Разбан/Открыть аккаунт",
            "generate_code_button": "Сгенерировать код",
            "code_label": "Код восстановления (12 символов)",
            "code_hint": "Введите 12-символьный код",
            "reason_label": "Причина бана",
            "reason_hint": "Введите причину бана",
            "ban_duration_label": "Длительность бана",
            "ban_duration_hint": "Выберите длительность",
            "ban_permanent": "Перманентный бан",
            "ban_custom_days": "Указать в днях",
            "ban_days_label": "Дни",
            "ban_days_hint": "Введите количество дней",
            "history_title": "Недавние действия",
            "refresh_tooltip": "Обновить данные",
            "table": {
                "id": "ID",
                "user": "Пользователь",
                "action": "Действие",
                "datetime": "Дата и время",
                "status": "Статус"
            },
            "actions": {
                "ban": "Бан",
                "lock": "Блокировка аккаунта",
                "unlock_unban": "Разбан/Открытие аккаунта",
                "warning": "Предупреждение",
                "verification": "Верификация"
            },
            "statuses": {
                "active": "Активен",
                "locked": "Заблокирован",
                "banned": "Забанен",
                "checking": "Проверяется"
            },
            "errors": {
                "wrong_hashtag": "Неверный формат хэштега",
                "wrong_characters": "Хэштег может содержать только: 0, 2, 8, 9, P, Y, L, Q, G, R, J, C, U, V",
                "no_hashtag": "Хэштег должен начинаться с #",
                "length_error": "Длина хэштега должна быть от 1 до 16 символов",
                "enter_valid_id": "Введите корректный ID пользователя",
                "code_length_error": "Код должен содержать ровно 12 символов",
                "enter_valid_code": "Введите корректный код восстановления",
                "enter_reason": "Введите причину бана",
                "grpc_error": "Error соединения с сервером",
                "connection_failed": "Не удалось подключиться к gRPC серверу",
                "invalid_days": "Введите корректное количество дней (1-3650)",
                "days_required": "Укажите количество дней для временного бана",
                "days_not_entered": "Введите количество дней для временного бана"
            },
            "success": {
                "user_banned": "Пользователь успешно забанен",
                "account_locked": "Аккаунт успешно закрыт. Код восстановления: {}",
                "account_unlocked_unbanned": "Аккаунт успешно разбанен/открыт"
            },
            "logs": {
                "ban_initiated": "Начало бана пользователя: {}",
                "ban_completed": "Бан пользователя завершен: {}",
                "lock_initiated": "Начало блокировки аккаунта: {}",
                "lock_completed": "Блокировка аккаунта завершена: {}",
                "unlock_initiated": "Начало разблокировки аккаунта: {}",
                "unlock_completed": "Разблокировка аккаунта завершена: {}",
                "connection_established": "Соединение с gRPC сервером установлено",
                "connection_lost": "Потеряно соединение с gRPC сервером",
                "validation_error": "Error валидации: {}",
                "server_error": "Error сервера: {}"
            }
        },
        "messages": {
            "title": "Сообщения",
            "subtitle": "Управление сообщениями пользователей",
            "count": "0",
            "card_title": "Сообщения",
            "development_text": "Раздел находится в разработке",
            "coming_soon": "Здесь будет управление сообщениями",
            "send_messages_title": "Отправить сообщения",
            "player_id_label": "ID игрока",
            "player_id_hint": "#0289PYLQGRJCUV (1-16 символов)",
            "messages_title": "Сообщения",
            "add_message_button": "Добавить сообщение",
            "send_messages_button": "Отправить сообщения",
            "message_label": "Сообщение",
            "message_hint": "Введите текст сообщения (3-128 символов)",
            "system_message_checkbox": "Системное сообщение",
            "errors": {
                "enter_valid_player_id": "Введите корректный ID игрока.",
                "message_length_error": "Длина сообщения должна быть от 3 до 128 символов",
                "grpc_connection_failed": "Error подключения к серверу сообщений."
            }
        },
        "gifts": {
            "title": "Подарки",
            "subtitle": "Управление подарками и наградами",
            "count": "0",
            "card_title": "Подарки и награды",
            "development_text": "Раздел находится в разработке",
            "coming_soon": "Здесь будет управление подарками",
            "send_gift_title": "Отправить подарок",
            "player_id_label": "ID игрока",
            "player_id_hint": "#0289PYLQGRJCUV (1-16 символов)",
            "items_title": "Предметы подарка",
            "add_item_button": "Добавить предмет",
            "send_gift_button": "Отправить подарок",
            "item_type_label": "Тип предмета",
            "item_quantity_label": "Количество",
            "item_quantity_hint": "1-99999",
            "fighter_name_label": "Имя бойца",
            "fighter_name_hint": "Введите имя бойца",
            "skin_name_label": "Имя скина",
            "skin_name_hint": "Введите имя скина",
            "errors": {
                "enter_valid_player_id": "Введите корректный ID игрока.",
                "enter_item_type": "Выберите тип предмета",
                "enter_item_quantity": "Введите количество предмета",
                "enter_fighter_name": "Введите имя бойца",
                "enter_skin_name": "Введите имя скина",
                "grpc_connection_failed": "Error подключения к серверу подарков."
            }
        },
        "shop": {
            "title": "Магазин",
            "subtitle": "Управление товарами и акциями",
            "count": "0",
            "card_title": "Магазин",
            "development_text": "Раздел находится в разработке",
            "coming_soon": "Здесь будет управление магазином",
            "create_promotion": "Создать акцию",
            "delete_promotion": "Удалить акцию",
            "promotion_history": "История акций",
            "create_promotion_title": "Создание новой акции",
            "delete_promotion_title": "Удаление акции",
            "promotion_history_title": "История акций",
            "promotion_id_label": "ID акции",
            "promotion_id_hint": "Введите ID акции",
            "promotion_name_label": "Название акции",
            "promotion_name_hint": "Введите название акции",
            "is_daily_label": "Дневная акция",
            "start_time_label": "Время начала (Unix timestamp)",
            "start_time_hint": "Введите время начала в формате Unix timestamp",
            "end_time_label": "Время окончания (Unix timestamp)",
            "end_time_hint": "Введите время окончания в формате Unix timestamp",
            "price_label": "Цена",
            "price_hint": "Введите цену акции",
            "old_price_label": "Старая цена",
            "old_price_hint": "Введите старую цену акции",
            "price_type_label": "Тип цены",
            "background_label": "Фон акции",
            "background_hint": "Введите имя фона",
            "show_to_new_users_label": "Показывать новым пользователям",
            "items_title": "Предметы в акции",
            "add_item_button": "Добавить предмет",
            "remove_item_button": "Удалить предмет",
            "item_type_label": "Тип предмета",
            "item_quantity_label": "Количество",
            "item_quantity_hint": "1-99999",
            "fighter_name_label": "Имя бойца",
            "fighter_name_hint": "Введите имя бойца",
            "skin_name_label": "Имя скина",
            "skin_name_hint": "Введите имя скина",
            "create_button": "Создать",
            "delete_button": "Удалить",
            "cancel_button": "Отмена",
            "back_button": "Назад",
            "no_promotions": "Акции не найдены",
            "success_title": "Успех",
            "error_title": "Error",
            "item_types": {
                "3": "Боец",
                "4": "Скин бойца",
                "0": "Ящик",
                "1": "Золото",
                "2": "Ключи",
                "5": "Монеты",
                "6": "Кристаллы",
                "7": "Билеты",
                "8": "Очки силы",
                "9": "Удвоитель жетонов",
                "10": "Мегаящик",
                "11": "Очки звездности",
                "12": "Очки силы (сезонные)",
                "13": "Звёздные Очки",
                "14": "Большой ящик",
                "15": "Сезонные билеты",
                "16": "Алмазы",
                "17": "Звездные Поинты"
            },
            "price_types": {
                "0": "За кристаллы",
                "1": "За золото",
                "2": "За просмотр",
                "3": "За звездные поинты",
                "4": "Нельзя купить"
            },
            "errors": {
                "enter_promotion_id": "Введите ID акции",
                "promotion_not_found": "Акция с указанным ID не найдена",
                "enter_promotion_name": "Введите название акции",
                "enter_start_time": "Введите время начала",
                "enter_end_time": "Введите время окончания",
                "enter_price": "Введите цену акции",
                "enter_old_price": "Введите старую цену акции",
                "select_price_type": "Выберите тип цены",
                "enter_background": "Введите фон акции",
                "grpc_connection_failed": "Error подключения к серверу магазина",
                "incorrect_promotion_id": "Неверный формат ID акции!",
                "item_validation_failed": "Error валидации предметов в акции"
            },
            "copied": "скопировано"
        },
        "statistics": {
            "title": "Статистика",
            "subtitle": "Аналитика и отчеты",
            "live_status": "LIVE",
            "global_stats": {
                "title": "Глобальная статистика",
                "active_players": "Активных игроков",
                "active_battles": "Активных боев",
                "players": "Игроков",
                "alliances": "Альянсов",
                "game_rooms": "Игровых комнат"
            },
            "player_search": {
                "title": "Поиск статистики игрока",
                "view_button": "Просмотр"
            },
            "player_stats": {
                "title": "Статистика игрока",
                "ban_info": "Информация о бане",
                "is_banned": "Забанен",
                "ban_reason": "Причина",
                "ban_end_time": "Время окончания",
                "lock_info": "Информация о блокировке",
                "is_locked": "Аккаунт закрыт",
                "unlock_code": "Код разблокировки",
                "general_info": "Общая информация",
                "sessions_count": "Количество сессий",
                "creation_time": "Дата создания аккаунта",
                "play_time": "Время в игре (секунды)",
                "device_language": "Язык устройства",
                "devices_info": "Информация о устройствах",
                "servers_info": "Серверы подключения",
                "clients_info": "IP адреса клиентов",
                "no": "Нет",
                "yes": "Да"
            },
            "connections": "подключений",
            "entries": "заходов"
        }
    }),
    "en": json.dumps({
        "app_title": "Admin Panel",
        "menu": {
            "title_main": "ADMIN",
            "title_sub": "DASHBOARD",
            "moderation": "Moderation",
            "messages": "Messages",
            "gifts": "Gifts",
            "shop": "Shop",
            "statistics": "Statistics",
            "system_status": "SYSTEM",
            "system_online": "Online",
            "system_version": "Version ZOVserver V24"
        },
        "moderation": {
            "title": "Moderation",
            "subtitle": "User management and actions",
            "live_status": "LIVE",
            "actions_card_title": "User Actions",
            "user_id_label": "User ID",
            "user_id_hint": "#0289PYLQGRJCUV (1-16 characters)",
            "ban_button": "Ban",
            "lock_button": "Lock Account",
            "unlock_unban_button": "Unban/Unlock Account",
            "generate_code_button": "Generate Code",
            "code_label": "Recovery Code (12 characters)",
            "code_hint": "Enter 12-character code",
            "reason_label": "Ban Reason",
            "reason_hint": "Enter ban reason",
            "ban_duration_label": "Ban Duration",
            "ban_duration_hint": "Select duration",
            "ban_permanent": "Permanent Ban",
            "ban_custom_days": "Specify in days",
            "ban_days_label": "Days",
            "ban_days_hint": "Enter number of days",
            "history_title": "Recent Actions",
            "refresh_tooltip": "Refresh Data",
            "table": {
                "id": "ID",
                "user": "User",
                "action": "Action",
                "datetime": "Date & Time",
                "status": "Status"
            },
            "actions": {
                "ban": "Ban",
                "lock": "Account Lock",
                "unlock_unban": "Unban/Unlock Account",
                "warning": "Warning",
                "verification": "Verification"
            },
            "statuses": {
                "active": "Active",
                "locked": "Locked",
                "banned": "Banned",
                "checking": "Checking"
            },
            "errors": {
                "wrong_hashtag": "Wrong hashtag format",
                "wrong_characters": "Hashtag can only contain: 0, 2, 8, 9, P, Y, L, Q, G, R, J, C, U, V",
                "no_hashtag": "Hashtag must start with #",
                "length_error": "Hashtag length must be 1-16 characters",
                "enter_valid_id": "Enter a valid user ID",
                "code_length_error": "Code must be exactly 12 characters",
                "enter_valid_code": "Enter a valid recovery code",
                "enter_reason": "Enter ban reason",
                "grpc_error": "Server connection error",
                "connection_failed": "Failed to connect to gRPC server",
                "invalid_days": "Enter a valid number of days (1-3650)",
                "days_required": "Specify days for temporary ban",
                "days_not_entered": "Enter number of days for temporary ban"
            },
            "success": {
                "user_banned": "User successfully banned",
                "account_locked": "Account successfully locked. Recovery code: {}",
                "account_unlocked_unbanned": "Account successfully unbanned/unlocked",
            },
            "logs": {
                "ban_initiated": "User ban initiated: {}",
                "ban_completed": "User ban completed: {}",
                "lock_initiated": "Account lock initiated: {}",
                "lock_completed": "Account lock completed: {}",
                "unlock_initiated": "Account unlock initiated: {}",
                "unlock_completed": "Account unlock completed: {}",
                "connection_established": "Connection to gRPC server established",
                "connection_lost": "Lost connection to gRPC server",
                "validation_error": "Validation error: {}",
                "server_error": "Server error: {}"
            }
        },
        "messages": {
            "title": "Messages",
            "subtitle": "User message management",
            "count": "0",
            "card_title": "Messages",
            "development_text": "Section under development",
            "coming_soon": "Message management will be here",
            "send_messages_title": "Send Messages",
            "player_id_label": "Player ID",
            "player_id_hint": "#0289PYLQGRJCUV (1-16 characters)",
            "messages_title": "Messages",
            "add_message_button": "Add Message",
            "send_messages_button": "Send Messages",
            "message_label": "Message",
            "message_hint": "Enter message text (3-128 characters)",
            "system_message_checkbox": "System Message",
            "errors": {
                "enter_valid_player_id": "Enter a valid player ID.",
                "message_length_error": "Message length must be between 3 and 128 characters",
                "grpc_connection_failed": "Failed to connect to messages server."
            }
        },
        "gifts": {
            "title": "Gifts",
            "subtitle": "Gift and reward management",
            "count": "0",
            "card_title": "Gifts & Rewards",
            "development_text": "Section under development",
            "coming_soon": "Gift management will be here",
            "send_gift_title": "Send Gift",
            "player_id_label": "Player ID",
            "player_id_hint": "#0289PYLQGRJCUV (1-16 characters)",
            "items_title": "Gift Items",
            "add_item_button": "Add Item",
            "send_gift_button": "Send Gift",
            "item_type_label": "Item Type",
            "item_quantity_label": "Quantity",
            "item_quantity_hint": "1-99999",
            "fighter_name_label": "Brawler Name",
            "fighter_name_hint": "Enter brawler name",
            "skin_name_label": "Skin Name",
            "skin_name_hint": "Enter skin name",
            "errors": {
                "enter_valid_player_id": "Enter a valid player ID.",
                "enter_item_type": "Select item type",
                "enter_item_quantity": "Enter item quantity",
                "enter_fighter_name": "Enter brawler name",
                "enter_skin_name": "Enter skin name",
                "grpc_connection_failed": "Failed to connect to gifts server."
            }
        },
        "shop": {
            "title": "Shop",
            "subtitle": "Product and offer management",
            "count": "0",
            "card_title": "Shop",
            "development_text": "Section under development",
            "coming_soon": "Shop management will be here",
            "create_promotion": "Create Promotion",
            "delete_promotion": "Delete Promotion",
            "promotion_history": "Promotion History",
            "create_promotion_title": "Create New Promotion",
            "delete_promotion_title": "Delete Promotion",
            "promotion_history_title": "Promotion History",
            "promotion_id_label": "Promotion ID",
            "promotion_id_hint": "Enter promotion ID",
            "promotion_name_label": "Promotion Name",
            "promotion_name_hint": "Enter promotion name",
            "is_daily_label": "Daily Promotion",
            "start_time_label": "Start Time (Unix timestamp)",
            "start_time_hint": "Enter start time as Unix timestamp",
            "end_time_label": "End Time (Unix timestamp)",
            "end_time_hint": "Enter end time as Unix timestamp",
            "price_label": "Price",
            "price_hint": "Enter promotion price",
            "old_price_label": "Old Price",
            "old_price_hint": "Enter old promotion price",
            "price_type_label": "Price Type",
            "background_label": "Promotion Background",
            "background_hint": "Enter background name",
            "show_to_new_users_label": "Show to New Users",
            "items_title": "Items in Promotion",
            "add_item_button": "Add Item",
            "remove_item_button": "Remove Item",
            "item_type_label": "Item Type",
            "item_quantity_label": "Quantity",
            "item_quantity_hint": "1-99999",
            "fighter_name_label": "Brawler Name",
            "fighter_name_hint": "Enter brawler name",
            "skin_name_label": "Skin Name",
            "skin_name_hint": "Enter skin name",
            "create_button": "Create",
            "delete_button": "Delete",
            "cancel_button": "Cancel",
            "back_button": "Back",
            "no_promotions": "No promotions found",
            "success_title": "Success",
            "error_title": "Error",
            "item_types": {
                "3": "Brawler",
                "4": "Brawler Skin",
                "0": "Box",
                "1": "Gold",
                "2": "Keys",
                "5": "Coins",
                "6": "Gems",
                "7": "Tickets",
                "8": "Power Points",
                "9": "Token Doubler",
                "10": "Mega Box",
                "11": "Star Points",
                "12": "Season Power Points",
                "13": "Star Tokens",
                "14": "Big Box",
                "15": "Season Tickets",
                "16": "Diamonds",
                "17": "Star Points"
            },
            "price_types": {
                "0": "For Gems",
                "1": "For Gold",
                "2": "For View",
                "3": "For Star Points",
                "4": "Cannot Buy"
            },
            "errors": {
                "enter_promotion_id": "Enter promotion ID",
                "promotion_not_found": "Promotion with specified ID not found",
                "enter_promotion_name": "Enter promotion name",
                "enter_start_time": "Enter start time",
                "enter_end_time": "Enter end time",
                "enter_price": "Enter promotion price",
                "enter_old_price": "Enter old promotion price",
                "select_price_type": "Select price type",
                "enter_background": "Enter promotion background",
                "grpc_connection_failed": "Failed to connect to shop server",
                "incorrect_promotion_id": "Incorrect promotion ID format!",
                "item_validation_failed": "Item validation failed in promotion"
            },
            "copied": "copied"
        },
        "statistics": {
            "title": "Statistics",
            "subtitle": "Analytics and reports",
            "live_status": "LIVE",
            "global_stats": {
                "title": "Global Statistics",
                "active_players": "Active Players",
                "active_battles": "Active Battles",
                "players": "Players",
                "alliances": "Alliances",
                "game_rooms": "Game Rooms"
            },
            "player_search": {
                "title": "Player Statistics Search",
                "view_button": "View"
            },
            "player_stats": {
                "title": "Player Statistics",
                "ban_info": "Ban Information",
                "is_banned": "Banned",
                "ban_reason": "Reason",
                "ban_end_time": "End Time",
                "lock_info": "Lock Information",
                "is_locked": "Account Locked",
                "unlock_code": "Unlock Code",
                "general_info": "General Information",
                "sessions_count": "Sessions Count",
                "creation_time": "Account Creation Time",
                "play_time": "Play Time (seconds)",
                "device_language": "Device Language",
                "devices_info": "Device Information",
                "servers_info": "Connected Servers",
                "clients_info": "Client IPs",
                "no": "No",
                "yes": "Yes"
            },
            "connections": "connections",
            "entries": "entries"
        }
    }),
    "zh": json.dumps({
        "app_title": "管理面板",
        "menu": {
            "title_main": "管理员",
            "title_sub": "仪表板",
            "moderation": "审核",
            "messages": "消息",
            "gifts": "礼物",
            "shop": "商店",
            "statistics": "统计",
            "system_status": "系统",
            "system_online": "在线",
            "system_version": "版本 ZOVserver V24"
        },
        "moderation": {
            "title": "审核",
            "subtitle": "用户管理和操作",
            "live_status": "实时",
            "actions_card_title": "用户操作",
            "user_id_label": "用户ID",
            "user_id_hint": "#0289PYLQGRJCUV (1-16字符)",
            "ban_button": "封禁",
            "lock_button": "锁定账户",
            "unlock_unban_button": "解封/解锁账户",
            "generate_code_button": "生成代码",
            "code_label": "恢复代码 (12个字符)",
            "code_hint": "输入12位代码",
            "reason_label": "封禁原因",
            "reason_hint": "输入封禁原因",
            "ban_duration_label": "封禁时长",
            "ban_duration_hint": "选择时长",
            "ban_permanent": "永久封禁",
            "ban_custom_days": "自定义天数",
            "ban_days_label": "天数",
            "ban_days_hint": "输入天数",
            "history_title": "最近操作",
            "refresh_tooltip": "刷新数据",
            "table": {
                "id": "ID",
                "user": "用户",
                "action": "操作",
                "datetime": "日期时间",
                "status": "状态"
            },
            "actions": {
                "ban": "封禁",
                "lock": "账户锁定",
                "unlock_unban": "解封/账户解锁",
                "warning": "警告",
                "verification": "验证"
            },
            "statuses": {
                "active": "活跃",
                "locked": "已锁定",
                "banned": "已封禁",
                "checking": "检查中"
            },
            "errors": {
                "wrong_hashtag": "错误的标签格式",
                "wrong_characters": "标签只能包含: 0, 2, 8, 9, P, Y, L, Q, G, R, J, C, U, V",
                "no_hashtag": "标签必须以#开头",
                "length_error": "标签长度必须为1-16个字符",
                "enter_valid_id": "请输入有效的用户ID",
                "code_length_error": "代码必须正好12个字符",
                "enter_valid_code": "请输入有效的恢复代码",
                "enter_reason": "请输入封禁原因",
                "grpc_error": "服务器连接错误",
                "connection_failed": "无法连接到gRPC服务器",
                "invalid_days": "请输入有效的天数 (1-3650)",
                "days_required": "请指定临时封禁的天数",
                "days_not_entered": "请输入临时封禁的天数"
            },
            "success": {
                "user_banned": "用户已成功封禁",
                "account_locked": "账户已成功锁定。恢复代码: {}",
                "account_unlocked_unbanned": "账户已成功解封/解锁"
            },
            "logs": {
                "ban_initiated": "开始封禁用户: {}",
                "ban_completed": "用户封禁完成: {}",
                "lock_initiated": "开始锁定账户: {}",
                "lock_completed": "账户锁定完成: {}",
                "unlock_initiated": "开始解锁账户: {}",
                "unlock_completed": "账户解锁完成: {}",
                "connection_established": "已连接到gRPC服务器",
                "connection_lost": "与gRPC服务器失去连接",
                "validation_error": "验证错误: {}",
                "server_error": "服务器错误: {}"
            }
        },
        "messages": {
            "title": "消息",
            "subtitle": "用户消息管理",
            "count": "0",
            "card_title": "消息",
            "development_text": "开发中",
            "coming_soon": "消息管理功能即将上线",
            "send_messages_title": "发送消息",
            "player_id_label": "玩家ID",
            "player_id_hint": "#0289PYLQGRJCUV (1-16字符)",
            "messages_title": "消息",
            "add_message_button": "添加消息",
            "send_messages_button": "发送消息",
            "message_label": "消息",
            "message_hint": "输入消息文本 (3-128字符)",
            "system_message_checkbox": "系统消息",
            "errors": {
                "enter_valid_player_id": "请输入有效的玩家ID。",
                "message_length_error": "消息长度必须在3到128个字符之间",
                "grpc_connection_failed": "连接消息服务器失败。"
            }
        },
        "gifts": {
            "title": "礼物",
            "subtitle": "礼物和奖励管理",
            "count": "0",
            "card_title": "礼物和奖励",
            "development_text": "开发中",
            "coming_soon": "礼物管理功能即将上线",
            "send_gift_title": "发送礼物",
            "player_id_label": "玩家ID",
            "player_id_hint": "#0289PYLQGRJCUV (1-16字符)",
            "items_title": "礼物物品",
            "add_item_button": "添加物品",
            "send_gift_button": "发送礼物",
            "item_type_label": "物品类型",
            "item_quantity_label": "数量",
            "item_quantity_hint": "1-99999",
            "fighter_name_label": "战士名称",
            "fighter_name_hint": "输入战士名称",
            "skin_name_label": "皮肤名称",
            "skin_name_hint": "输入皮肤名称",
            "errors": {
                "enter_valid_player_id": "请输入有效的玩家ID。",
                "enter_item_type": "选择物品类型",
                "enter_item_quantity": "输入物品数量",
                "enter_fighter_name": "输入战士名称",
                "enter_skin_name": "输入皮肤名称",
                "grpc_connection_failed": "连接礼物服务器失败。"
            }
        },
        "shop": {
            "title": "商店",
            "subtitle": "商品和购买管理",
            "count": "0",
            "card_title": "商店",
            "development_text": "开发中",
            "coming_soon": "商店管理功能即将上线",
            "create_promotion": "创建促销",
            "delete_promotion": "删除促销",
            "promotion_history": "促销历史",
            "create_promotion_title": "创建新促销",
            "delete_promotion_title": "删除促销",
            "promotion_history_title": "促销历史",
            "promotion_id_label": "促销ID",
            "promotion_id_hint": "输入促销ID",
            "promotion_name_label": "促销名称",
            "promotion_name_hint": "输入促销名称",
            "is_daily_label": "每日促销",
            "start_time_label": "开始时间 (Unix时间戳)",
            "start_time_hint": "输入Unix时间戳格式的开始时间",
            "end_time_label": "结束时间 (Unix时间戳)",
            "end_time_hint": "输入Unix时间戳格式的结束时间",
            "price_label": "价格",
            "price_hint": "输入促销价格",
            "old_price_label": "原价",
            "old_price_hint": "输入促销原价",
            "price_type_label": "价格类型",
            "background_label": "促销背景",
            "background_hint": "输入背景",
            "show_to_new_users_label": "向新用户展示",
            "items_title": "促销中的物品",
            "add_item_button": "添加物品",
            "remove_item_button": "移除物品",
            "item_type_label": "物品类型",
            "item_quantity_label": "数量",
            "item_quantity_hint": "1-99999",
            "fighter_name_label": "战士名称",
            "fighter_name_hint": "输入战士名称",
            "skin_name_label": "皮肤名称",
            "skin_name_hint": "输入皮肤名称",
            "create_button": "创建",
            "delete_button": "删除",
            "cancel_button": "取消",
            "back_button": "返回",
            "no_promotions": "未找到促销活动",
            "success_title": "成功",
            "error_title": "错误",
            "item_types": {
                "3": "战士",
                "4": "战士皮肤",
                "0": "箱子",
                "1": "金币",
                "2": "钥匙",
                "5": "硬币",
                "6": "宝石",
                "7": "门票",
                "8": "力量点数",
                "9": "双倍令牌",
                "10": "超级箱子",
                "11": "星星点数",
                "12": "赛季力量点数",
                "13": "星星代币",
                "14": "大箱子",
                "15": "赛季门票",
                "16": "钻石",
                "17": "星星点数"
            },
            "price_types": {
                "0": "宝石购买",
                "1": "金币购买",
                "2": "观看获得",
                "3": "星星点数购买",
                "4": "无法购买"
            },
            "errors": {
                "enter_promotion_id": "请输入促销ID",
                "promotion_not_found": "未找到指定ID的促销活动",
                "enter_promotion_name": "请输入促销名称",
                "enter_start_time": "请输入开始时间",
                "enter_end_time": "请输入结束时间",
                "enter_price": "请输入促销价格",
                "enter_old_price": "请输入促销原价",
                "select_price_type": "请选择价格类型",
                "enter_background": "请输入促销背景",
                "grpc_connection_failed": "连接商店服务器失败",
                "incorrect_promotion_id": "促销ID格式不正确!",
                "item_validation_failed": "促销物品验证失败"
            },
            "copied": "复制的"
        },
        "statistics": {
            "title": "统计",
            "subtitle": "分析和报告",
            "live_status": "实时",
            "global_stats": {
                "title": "全局统计",
                "active_players": "活跃玩家",
                "active_battles": "积极的战斗",
                "players": "球员们",
                "alliances": "联盟",
                "game_rooms": "游戏房间"
            },
            "player_search": {
                "title": "玩家统计查询",
                "view_button": "查看"
            },
            "player_stats": {
                "title": "玩家统计",
                "ban_info": "封禁信息",
                "is_banned": "已封禁",
                "ban_reason": "原因",
                "ban_end_time": "结束时间",
                "lock_info": "锁定信息",
                "is_locked": "账户已锁定",
                "unlock_code": "解锁代码",
                "general_info": "基本信息",
                "sessions_count": "会话次数",
                "creation_time": "账户创建时间",
                "play_time": "游戏时间(秒)",
                "device_language": "设备语言",
                "devices_info": "设备信息",
                "servers_info": "连接服务器",
                "clients_info": "客户端IP",
                "no": "否",
                "yes": "是"
            },
            "connections": "连接",
            "entries": "进入"
        }
    })
}

STYLES_CONFIG = json.dumps({
    "colors": {
        "primary": "#bb86fc",
        "primary_variant": "#3700b3",
        "secondary": "#03dac6",
        "background": "#121212",
        "surface": "#1e1e1e",
        "menu_bg": "#1e1e1e",
        "menu_item_bg": "#2d2d2d",
        "menu_item_hover": "#3d3d3d",
        "menu_item_active": "#333333",
        "text_primary": "#ffffff",
        "text_secondary": "#b3b3b3",
        "divider": "#333333",
        "success": "#4caf50",
        "error": "#f44336",
        "warning": "#ff9800",
        "info": "#2196f3",
        "button_disabled": "#616161"
    },
    "sizes": {
        "menu_width": 230,
        "card_border_radius": 16,
        "button_border_radius": 8,
        "menu_button_padding": [16, 14, 16, 14],
        "card_padding": 30
    }
})


class HashtagValidator:
    TAG_CHAR = ("0", "2", "8", "9", "P", "Y", "L", "Q", "G", "R", "J", "C", "U", "V")

    @staticmethod
    def validate_hashtag(hashtag):
        if not hashtag:
            return False, "empty"
        if not hashtag.startswith('#'):
            return False, "no_hashtag"
        tag_content = hashtag[1:].upper()

        if len(tag_content) < 1 or len(tag_content) > 16:
            return False, "length_error"

        for char in tag_content:
            if char not in HashtagValidator.TAG_CHAR:
                return False, "wrong_characters"
        return True, "valid"

    @staticmethod
    def get_id(hashtag):
        is_valid, error = HashtagValidator.validate_hashtag(hashtag)
        if not is_valid:
            return None

        tag_content = hashtag[1:].upper()
        tag_array = list(tag_content)
        id_value = 0
        for character in tag_array:
            char_index = HashtagValidator.TAG_CHAR.index(character)
            id_value *= len(HashtagValidator.TAG_CHAR)
            id_value += char_index

        return id_value

    @staticmethod
    def get_hl_id(hashtag):
        id_value = HashtagValidator.get_id(hashtag)
        if id_value is None:
            return None

        high_low = []
        high_low.append(id_value % 256)
        high_low.append((id_value - high_low[0]) >> 8)

        return tuple(high_low)


class HashtagFieldFactory:
    def __init__(self, page, locale, styles):
        self.page = page
        self.locale = locale
        self.styles = styles

    def create_hashtag_field(self, label, hint_text, on_valid_change=None):
        text_field = ft.TextField(
            label=label,
            hint_text=hint_text,
            width=320,
            border_radius=8,
            border_color=self.styles["colors"]["divider"],
            focused_border_color=self.styles["colors"]["primary"],
            prefix_icon="person"
        )
        error_text = ft.Text(
            size=12,
            color=self.styles["colors"]["error"],
            visible=False
        )
        id_display = ft.Text(
            size=14,
            color=self.styles["colors"]["info"],
            visible=False
        )

        current_valid_data = {
            'high_low_id': None,
            'numeric_id': None,
            'original': None
        }

        def on_text_change(e):
            error_text.visible = False
            self.page.update()

            hashtag = text_field.value
            if hashtag and hashtag.strip():
                is_valid, error_type = HashtagValidator.validate_hashtag(hashtag)

                if is_valid:
                    numeric_id = HashtagValidator.get_id(hashtag)
                    hl_id = HashtagValidator.get_hl_id(hashtag)
                    if numeric_id is not None and hl_id is not None:
                        current_valid_data['high_low_id'] = hl_id
                        current_valid_data['numeric_id'] = numeric_id
                        current_valid_data['original'] = hashtag

                        id_display.value = f"ID: {numeric_id} | HighLow: {hl_id[0]}-{hl_id[1]}"
                        id_display.visible = True
                        error_text.visible = False

                        if on_valid_change:
                            on_valid_change(current_valid_data)
                    else:
                        error_text.value = self.locale["moderation"]["errors"]["wrong_hashtag"]
                        error_text.visible = True
                        id_display.visible = False
                        current_valid_data['high_low_id'] = None
                        current_valid_data['numeric_id'] = None
                        current_valid_data['original'] = None
                        if on_valid_change:
                            on_valid_change(None)
                else:
                    error_messages = self.locale["moderation"]["errors"]
                    error_text.value = error_messages.get(error_type, error_messages["wrong_hashtag"])
                    error_text.visible = True
                    id_display.visible = False
                    current_valid_data['high_low_id'] = None
                    current_valid_data['numeric_id'] = None
                    current_valid_data['original'] = None
                    if on_valid_change:
                        on_valid_change(None)
            else:
                error_text.visible = False
                id_display.visible = False
                current_valid_data['high_low_id'] = None
                current_valid_data['numeric_id'] = None
                current_valid_data['original'] = None

                if on_valid_change:
                    on_valid_change(None)

            self.page.update()

        def get_current_data():
            return current_valid_data if current_valid_data['high_low_id'] else None

        def is_valid():
            return current_valid_data['high_low_id'] is not None

        text_field.on_change = on_text_change

        container = ft.Column([
            text_field,
            error_text,
            id_display
        ], spacing=5)

        container.get_current_data = get_current_data
        container.is_valid = is_valid
        container.text_field = text_field
        container.error_text = error_text
        container.id_display = id_display
        container.current_valid_data = current_valid_data

        return container


class CodeFieldFactory:
    def __init__(self, page, locale, styles):
        self.page = page
        self.locale = locale
        self.styles = styles

    def create_code_field(self, label, hint_text, on_valid_change=None):
        text_field = ft.TextField(
            label=label,
            hint_text=hint_text,
            width=320,
            border_radius=8,
            border_color=self.styles["colors"]["divider"],
            focused_border_color=self.styles["colors"]["primary"],
            prefix_icon="key"
        )
        error_text = ft.Text(
            size=12,
            color=self.styles["colors"]["error"],
            visible=False
        )

        current_valid_data = {
            'code': None
        }

        def on_text_change(e):
            error_text.visible = False
            self.page.update()

            code = text_field.value
            if code and code.strip():
                if len(code) != 12:
                    error_text.value = self.locale["moderation"]["errors"]["code_length_error"]
                    error_text.visible = True
                    current_valid_data['code'] = None
                    if on_valid_change:
                        on_valid_change(None)
                else:
                    current_valid_data['code'] = code
                    error_text.visible = False
                    if on_valid_change:
                        on_valid_change(current_valid_data)
            else:
                error_text.visible = False
                current_valid_data['code'] = None
                if on_valid_change:
                    on_valid_change(None)
            self.page.update()

        def get_current_data():
            return current_valid_data if current_valid_data['code'] else None

        def is_valid():
            return current_valid_data['code'] is not None and len(current_valid_data['code']) == 12

        text_field.on_change = on_text_change

        container = ft.Column([
            text_field,
            error_text
        ], spacing=5)

        container.get_current_data = get_current_data
        container.is_valid = is_valid
        container.text_field = text_field
        container.error_text = error_text
        container.current_valid_data = current_valid_data
        return container


class DaysFieldFactory:
    def __init__(self, page, locale, styles):
        self.page = page
        self.locale = locale
        self.styles = styles

    def create_days_field(self, label, hint_text, on_valid_change=None):
        text_field = ft.TextField(
            label=label,
            hint_text=hint_text,
            width=320,
            border_radius=8,
            border_color=self.styles["colors"]["divider"],
            focused_border_color=self.styles["colors"]["primary"],
            prefix_icon="calendar_today",
            keyboard_type=ft.KeyboardType.NUMBER
        )
        error_text = ft.Text(
            size=12,
            color=self.styles["colors"]["error"],
            visible=False
        )

        current_valid_data = {
            'days': None
        }

        def on_text_change(e):
            error_text.visible = False
            self.page.update()

            days_str = text_field.value
            if days_str and days_str.strip():
                try:
                    days = int(days_str)
                    if days < 1 or days > 3650:
                        error_text.value = self.locale["moderation"]["errors"]["invalid_days"]
                        error_text.visible = True
                        current_valid_data['days'] = None
                        if on_valid_change:
                            on_valid_change(None)
                    else:
                        current_valid_data['days'] = days
                        error_text.visible = False
                        if on_valid_change:
                            on_valid_change(current_valid_data)
                except ValueError:
                    error_text.value = self.locale["moderation"]["errors"]["invalid_days"]
                    error_text.visible = True
                    current_valid_data['days'] = None
                    if on_valid_change:
                        on_valid_change(None)
            else:
                error_text.visible = False
                current_valid_data['days'] = None
                if on_valid_change:
                    on_valid_change(None)
            self.page.update()

        def get_current_data():
            return current_valid_data if current_valid_data['days'] else None

        def is_valid():
            return current_valid_data['days'] is not None and 1 <= current_valid_data['days'] <= 3650

        text_field.on_change = on_text_change

        container = ft.Column([
            text_field,
            error_text
        ], spacing=5)

        container.get_current_data = get_current_data
        container.is_valid = is_valid
        container.text_field = text_field
        container.error_text = error_text
        container.current_valid_data = current_valid_data
        return container


class UserManagementClient:
    def __init__(self, server_address=ADMIN_PANEL_BACKEND_ADDRESS):
        self.server_address = server_address
        self.channel = None
        self.stub = None
        self.connected = False
        self.logger = logging.getLogger(__name__)

    def connect(self):
        try:
            self.channel = grpc.insecure_channel(self.server_address)
            self.stub = user_service_pb2_grpc.UserManagementStub(self.channel)
            self.connected = True

            return True
        except Exception as e:

            self.connected = False
            return False

    def ban_user(self, user_data, reason, unban_timestamp):
        try:
            if not self.connected or not self.stub:
                raise Exception("GRPC not connected")

            hl_id_obj = user_service_pb2.HighLowId(
                high=user_data['high_low_id'][0],
                low=user_data['high_low_id'][1]
            )

            request = user_service_pb2.BanUserRequest(
                user_id=user_data['original'],
                high_low_id=hl_id_obj,
                reason=reason,
                unban_timestamp=unban_timestamp
            )

            response = self.stub.BanUser(request)

            return response.success, response.message
        except grpc.RpcError as e:

            return False, f"gRPC error: {e.details()}"
        except Exception as e:

            return False, f"Error: {str(e)}"

    def lock_user_account(self, user_data, recovery_code):
        try:
            if not self.connected or not self.stub:
                raise Exception("GRPC not connected")

            hl_id_obj = user_service_pb2.HighLowId(
                high=user_data['high_low_id'][0],
                low=user_data['high_low_id'][1]
            )

            request = user_service_pb2.LockUserRequest(
                user_id=user_data['original'],
                high_low_id=hl_id_obj,
                recovery_code=recovery_code
            )

            response = self.stub.LockUserAccount(request)

            return response.success, response.message, response.recovery_code
        except grpc.RpcError as e:

            return False, f"gRPC error: {e.details()}", ""
        except Exception as e:

            return False, f"Error: {str(e)}", ""

    def unlock_user_account(self, user_data):
        try:
            if not self.connected or not self.stub:
                raise Exception("GRPC not connected")

            hl_id_obj = user_service_pb2.HighLowId(
                high=user_data['high_low_id'][0],
                low=user_data['high_low_id'][1]
            )

            request = user_service_pb2.UserIdRequest(
                user_id=user_data['original'],
                high_low_id=hl_id_obj
            )

            response = self.stub.UnlockUserAccount(request)

            return response.success, response.message
        except grpc.RpcError as e:

            return False, f"gRPC error: {e.details()}"
        except Exception as e:

            return False, f"Error: {str(e)}"

    def close(self):
        if self.channel:
            self.channel.close()
        self.connected = False


class GiftsClient:
    def __init__(self, server_address=ADMIN_PANEL_BACKEND_ADDRESS):
        self.server_address = server_address
        self.channel = None
        self.stub = None
        self.connected = False
        self.logger = logging.getLogger(__name__)

    def connect(self):
        try:
            self.channel = grpc.insecure_channel(self.server_address)
            self.stub = gifts_service_pb2_grpc.GiftsServiceStub(self.channel)
            self.connected = True

            return True
        except Exception as e:

            self.connected = False
            return False

    def send_gift(self, player_data, items_info, reason="Admin gift"):
        try:
            if not self.connected or not self.stub:
                raise Exception("Gifts Shop GRPC not connected")

            hl_id_obj = gifts_service_pb2.HighLowId(
                high=int(player_data['high_low_id'][0]),
                low=int(player_data['high_low_id'][1])
            )

            gift_items = []
            for item_info in items_info:
                item_type_map = {
                    1: gifts_service_pb2.GIFT_ITEM_TYPE_GOLD,
                    16: gifts_service_pb2.GIFT_ITEM_TYPE_DIAMONDS,
                    7: gifts_service_pb2.GIFT_ITEM_TYPE_TICKETS,
                    9: gifts_service_pb2.GIFT_ITEM_TYPE_TOKEN_DOUBLER,
                    17: gifts_service_pb2.GIFT_ITEM_TYPE_STAR_POINTS,
                    3: gifts_service_pb2.GIFT_ITEM_TYPE_BRAWLER,
                    4: gifts_service_pb2.GIFT_ITEM_TYPE_BRAWLER_SKIN,
                }

                item_type = item_type_map.get(item_info["type"], gifts_service_pb2.GIFT_ITEM_TYPE_UNSPECIFIED)

                gift_item = gifts_service_pb2.GiftItem(
                    item_type=item_type
                )

                if "quantity" in item_info:
                    gift_item.quantity = item_info["quantity"]

                if "brawler_name" in item_info:
                    gift_item.brawler_name = item_info["brawler_name"]

                if "skin_name" in item_info:
                    gift_item.skin_name = item_info["skin_name"]

                gift_items.append(gift_item)

            request = gifts_service_pb2.SendGiftRequest(
                player_id=hl_id_obj,
                items=gift_items,
                reason=reason
            )

            response = self.stub.SendGift(request)

            return response.success, response.message
        except grpc.RpcError as e:

            return False, f"gRPC error: {e.details()}"
        except Exception as e:

            return False, f"Error: {str(e)}"

    def close(self):
        if self.channel:
            self.channel.close()
        self.connected = False


class StatisticsClient:
    def __init__(self, server_address=ADMIN_PANEL_BACKEND_ADDRESS):
        self.server_address = server_address
        self.channel = None
        self.stub = None
        self.connected = False
        self.logger = logging.getLogger(__name__)

    def connect(self):
        try:
            self.channel = grpc.insecure_channel(self.server_address)
            self.stub = statistics_service_pb2_grpc.StatisticsServiceStub(self.channel)
            self.connected = True

            return True
        except Exception as e:

            self.connected = False
            return False

    def get_global_stats(self):
        try:
            if not self.connected or not self.stub:
                raise Exception("Statistics GRPC not connected")

            request = statistics_service_pb2.Empty()

            response = self.stub.GetGlobalStats(request)

            return {
                "success": response.success,
                "message": response.message,
                "active_players": response.active_players,
                "active_battles": response.active_battles,
                "players": response.players,
                "alliances": response.alliances,
                "game_rooms": response.game_rooms
            }
        except grpc.RpcError as e:

            return {
                "success": False,
                "message": f"gRPC error: {e.details()}",
                "players": 0,
                "active_players": 0,
                "active_battles": 0,
                "alliances": 0,
                "game_rooms": 0
            }
        except Exception as e:

            return {
                "success": False,
                "message": f"Error: {str(e)}",
                "players": 0,
                "active_players": 0,
                "active_battles": 0,
                "alliances": 0,
                "game_rooms": 0
            }

    def get_player_stats(self, user_data):
        try:
            if not self.connected or not self.stub:
                raise Exception("Statistics GRPC not connected")

            hl_id_obj = statistics_service_pb2.HighLowId(
                high=user_data['high_low_id'][0],
                low=user_data['high_low_id'][1]
            )

            request = statistics_service_pb2.PlayerStatsRequest(
                user_id=user_data['original'],
                high_low_id=hl_id_obj
            )

            response = self.stub.GetPlayerStats(request)

            devices_dict = dict(response.devices)
            servers_dict = dict(response.servers_connected)
            ips_dict = dict(response.client_ips)

            return {
                "success": response.success,
                "message": response.message,
                "is_banned": response.is_banned,
                "ban_reason": response.ban_reason,
                "ban_end_time": response.ban_end_time,
                "is_locked": response.is_locked,
                "unlock_code": response.unlock_code,
                "sessions_count": response.sessions_count,
                "creation_time": response.creation_time,
                "play_time_seconds": response.play_time_seconds,
                "device_language": response.device_language,
                "devices": devices_dict,
                "servers_connected": servers_dict,
                "client_ips": ips_dict
            }
        except grpc.RpcError as e:
            return {
                "success": False,
                "message": f"gRPC error: {e.details()}",
                "is_banned": False,
                "ban_reason": "",
                "ban_end_time": "",
                "is_locked": False,
                "unlock_code": "",
                "sessions_count": 0,
                "creation_time": "",
                "play_time_seconds": 0,
                "device_language": "",
                "devices": {},
                "servers_connected": {},
                "client_ips": {}
            }
        except Exception as e:
            return {
                "success": False,
                "message": f"Error: {str(e)}",
                "is_banned": False,
                "ban_reason": "",
                "ban_end_time": "",
                "is_locked": False,
                "unlock_code": "",
                "sessions_count": 0,
                "creation_time": "",
                "play_time_seconds": 0,
                "device_language": "",
                "devices": {},
                "servers_connected": {},
                "client_ips": {}
            }

    def close(self):
        if self.channel:
            self.channel.close()
        self.connected = False


class MessagesClient:
    def __init__(self, server_address=ADMIN_PANEL_BACKEND_ADDRESS):
        self.server_address = server_address
        self.channel = None
        self.stub = None
        self.connected = False
        self.logger = logging.getLogger(__name__)

    def connect(self):
        try:
            self.channel = grpc.insecure_channel(self.server_address)
            self.stub = messages_service_pb2_grpc.MessagesServiceStub(self.channel)
            self.connected = True

            return True
        except Exception as e:

            self.connected = False
            return False

    def send_messages(self, player_data, messages_info):
        try:
            if not self.connected or not self.stub:
                raise Exception("Messages GRPC not connected")

            hl_id_obj = messages_service_pb2.HighLowId(
                high=int(player_data['high_low_id'][0]),
                low=int(player_data['high_low_id'][1])
            )

            message_items = []
            for msg_info in messages_info:
                message_item = messages_service_pb2.MessageItem(
                    text=msg_info['text'],
                    from_support=msg_info.get('is_system', False)
                )
                message_items.append(message_item)

            request = messages_service_pb2.SendMessagesRequest(
                player_id=hl_id_obj,
                messages=message_items
            )

            response = self.stub.SendMessages(request)

            return response.success, response.message
        except grpc.RpcError as e:

            return False, f"gRPC error: {e.details()}"
        except Exception as e:

            return False, f"Error: {str(e)}"

    def close(self):
        if self.channel:
            self.channel.close()
        self.connected = False


class ShopServiceClient:
    def __init__(self, server_address=ADMIN_PANEL_BACKEND_ADDRESS):
        self.server_address = server_address
        self.channel = None
        self.stub = None
        self.connected = False
        self.logger = logging.getLogger(__name__)

    def connect(self):
        try:
            self.channel = grpc.insecure_channel(self.server_address)
            self.stub = shop_service_pb2_grpc.ShopServiceStub(self.channel)
            self.connected = True

            return True
        except Exception as e:

            self.connected = False
            return False

    def create_promotion(self, name, is_daily, start_time, end_time, price, old_price,
                         price_type, background, show_to_new_users, items_data):
        try:
            if not self.connected or not self.stub:
                raise Exception("Shop GRPC not connected")

            promotion_items = []
            for item_data in items_data:
                item = shop_service_pb2.PromotionItem(
                    item_type=item_data.get("type", 0),
                    quantity=item_data.get("quantity", 1),
                    brawler_name=item_data.get("brawler_name", ""),
                    skin_name=item_data.get("skin_name", "")
                )
                promotion_items.append(item)

            request = shop_service_pb2.CreatePromotionRequest(
                name=name,
                is_daily=is_daily,
                start_time=start_time,
                end_time=end_time,
                price=price,
                old_price=old_price,
                price_type=price_type,
                background=background,
                show_to_new_users=show_to_new_users,
                items=promotion_items
            )

            response = self.stub.CreatePromotion(request)

            return response.success, response.message, getattr(response, 'promotion_id', '')

        except grpc.RpcError as e:

            return False, f"gRPC error: {e.details()}", ""
        except Exception as e:

            return False, f"Error: {str(e)}", ""

    def delete_promotion(self, promotion_id):
        try:
            if not self.connected or not self.stub:
                raise Exception("Shop GRPC not connected")

            request = shop_service_pb2.DeletePromotionRequest(promotion_id=promotion_id)
            response = self.stub.DeletePromotion(request)

            return response.success, response.message

        except grpc.RpcError as e:

            return False, f"gRPC error: {e.details()}"
        except Exception as e:

            return False, f"Error: {str(e)}"

    def get_promotion_history(self):
        try:
            if not self.connected or not self.stub:
                raise Exception("Shop GRPC not connected")

            request = shop_service_pb2.GetPromotionHistoryRequest()
            response = self.stub.GetPromotionHistory(request)

            if response.success:
                promotions_data = [
                    {
                        "id": entry.promotion_id,
                        "name": entry.name,
                        "start_time": entry.start_time,
                        "end_time": entry.end_time
                    }
                    for entry in response.promotions
                ]
                return response.success, response.message, promotions_data
            else:
                return response.success, response.message, []

        except grpc.RpcError as e:

            return False, f"gRPC error: {e.details()}", []
        except Exception as e:

            return False, f"Error: {str(e)}", []

    def close(self):
        if self.channel:
            self.channel.close()
            self.connected = False


def main(page: ft.Page):
    current_locale = "en"
    
    user_client = UserManagementClient()
    user_client.connect()
    stats_client = StatisticsClient()
    stats_client.connect()
    shop_client = ShopServiceClient()
    shop_client.connect()
    messages_client = MessagesClient()
    connected = messages_client.connect()
    gifts_client = GiftsClient()
    connected = gifts_client.connect()

    def load_locale(locale_key):
        nonlocal current_locale
        current_locale = locale_key
        return json.loads(LOCALIZATIONS[locale_key])

    def load_styles():
        return json.loads(STYLES_CONFIG)

    locale = load_locale(current_locale)
    styles = load_styles()

    PRIMARY_COLOR = styles["colors"]["primary"]
    PRIMARY_VARIANT = styles["colors"]["primary_variant"]
    SECONDARY_COLOR = styles["colors"]["secondary"]
    BACKGROUND_COLOR = styles["colors"]["background"]
    SURFACE_COLOR = styles["colors"]["surface"]
    MENU_BG = styles["colors"]["menu_bg"]
    MENU_ITEM_BG = styles["colors"]["menu_item_bg"]
    MENU_ITEM_HOVER = styles["colors"]["menu_item_hover"]
    MENU_ITEM_ACTIVE = styles["colors"]["menu_item_active"]
    TEXT_PRIMARY = styles["colors"]["text_primary"]
    TEXT_SECONDARY = styles["colors"]["text_secondary"]
    DIVIDER_COLOR = styles["colors"]["divider"]
    SUCCESS_COLOR = styles["colors"]["success"]
    ERROR_COLOR = styles["colors"]["error"]
    WARNING_COLOR = styles["colors"]["warning"]
    INFO_COLOR = styles["colors"]["info"]
    BUTTON_DISABLED_COLOR = styles["colors"]["button_disabled"]
    CARD_BORDER_RADIUS = styles["sizes"]["card_border_radius"]
    BUTTON_BORDER_RADIUS = styles["sizes"]["button_border_radius"]
    MENU_WIDTH = styles["sizes"]["menu_width"]
    MENU_BUTTON_PADDING = styles["sizes"]["menu_button_padding"]
    CARD_PADDING = styles["sizes"]["card_padding"]

    page.title = locale["app_title"]
    
    base_dir = os.path.dirname(os.path.abspath(__file__))
    assets_path = os.path.join(base_dir, "assets")
    
    page.assets_dir = assets_path
    
    page.icon = "icon.png"
    page.window.icon = "icon.png"
    page.theme_mode = ft.ThemeMode.DARK
    page.window.width = 1200
    page.window.height = 800
    page.padding = 0
    page.spacing = 0

    button_style = ft.ButtonStyle(
        shape=ft.RoundedRectangleBorder(radius=BUTTON_BORDER_RADIUS),
        padding=ft.Padding(20, 14, 20, 14),
        elevation=4
    )
    menu_button_style = ft.ButtonStyle(
        shape=ft.RoundedRectangleBorder(radius=BUTTON_BORDER_RADIUS),
        padding=ft.Padding(*MENU_BUTTON_PADDING),
        bgcolor=MENU_ITEM_BG,
        color=TEXT_PRIMARY,
        overlay_color=MENU_ITEM_HOVER
    )

    current_view = ft.Container(expand=True, padding=30)
    active_menu_button = None

    def change_locale(locale_key):
        nonlocal locale, current_locale
        current_locale = locale_key
        locale = load_locale(locale_key)
        page.title = locale["app_title"]

        create_menu()

        if hasattr(page, 'current_view_func'):
            page.current_view_func()
        page.update()

    def show_moderation():
        hashtag_factory = HashtagFieldFactory(page, locale, styles)
        code_factory = CodeFieldFactory(page, locale, styles)
        days_factory = DaysFieldFactory(page, locale, styles)

        hashtag_field = hashtag_factory.create_hashtag_field(
            label=locale["moderation"]["user_id_label"],
            hint_text=locale["moderation"]["user_id_hint"]
        )
        code_field = code_factory.create_code_field(
            label=locale["moderation"]["code_label"],
            hint_text=locale["moderation"]["code_hint"]
        )
        days_field = days_factory.create_days_field(
            label=locale["moderation"]["ban_days_label"],
            hint_text=locale["moderation"]["ban_days_hint"]
        )

        reason_field = ft.TextField(
            label=locale["moderation"]["reason_label"],
            hint_text=locale["moderation"]["reason_hint"],
            width=320,
            border_radius=8,
            border_color=styles["colors"]["divider"],
            focused_border_color=styles["colors"]["primary"],
            prefix_icon="report"
        )

        ban_duration_dropdown = ft.Dropdown(
            label=locale["moderation"]["ban_duration_label"],
            hint_text=locale["moderation"]["ban_duration_hint"],
            width=320,
            border_radius=8,
            border_color=styles["colors"]["divider"],
            focused_border_color=styles["colors"]["primary"],
            options=[
                ft.dropdown.Option("permanent", locale["moderation"]["ban_permanent"]),
                ft.dropdown.Option("custom_days", locale["moderation"]["ban_custom_days"]),
            ],
            value="permanent"
        )

        ban_button = ft.ElevatedButton(
            content=ft.Row([
                ft.Icon("block", size=18, color="white"),
                ft.Text(locale["moderation"]["ban_button"], size=16, color="white")
            ], spacing=8, alignment=ft.CrossAxisAlignment.CENTER),
            bgcolor=BUTTON_DISABLED_COLOR,
            style=button_style,
            disabled=True,
            on_click=lambda _: handle_ban_action(
                hashtag_field.get_current_data(),
                reason_field.value,
                ban_duration_dropdown.value,
                days_field.get_current_data()
            )
        )
        lock_button = ft.ElevatedButton(
            content=ft.Row([
                ft.Icon("lock", size=18, color="white"),
                ft.Text(locale["moderation"]["lock_button"], size=16, color="white")
            ], spacing=8, alignment=ft.CrossAxisAlignment.CENTER),
            bgcolor=BUTTON_DISABLED_COLOR,
            style=button_style,
            disabled=True,
            on_click=lambda _: handle_lock_action(hashtag_field.get_current_data())
        )
        unlock_unban_button = ft.ElevatedButton(
            content=ft.Row([
                ft.Icon("lock_open", size=18, color="white"),
                ft.Text(locale["moderation"]["unlock_unban_button"], size=16, color="white")
            ], spacing=8, alignment=ft.CrossAxisAlignment.CENTER),
            bgcolor=BUTTON_DISABLED_COLOR,
            style=button_style,
            disabled=True,
            on_click=lambda _: handle_unlock_unban_action(hashtag_field.get_current_data())
        )
        generate_code_button = ft.ElevatedButton(
            content=ft.Row([
                ft.Icon("autorenew", size=18, color="white"),
                ft.Text(locale["moderation"]["generate_code_button"], size=16, color="white")
            ], spacing=8, alignment=ft.CrossAxisAlignment.CENTER),
            bgcolor=INFO_COLOR,
            style=button_style,
            on_click=lambda _: generate_recovery_code()
        )

        def update_button_states(valid_data):
            if valid_data is not None:
                ban_button.disabled = False
                ban_button.bgcolor = ERROR_COLOR
                lock_button.disabled = False
                lock_button.bgcolor = ERROR_COLOR
                unlock_unban_button.disabled = False
                unlock_unban_button.bgcolor = SUCCESS_COLOR
            else:
                ban_button.disabled = True
                ban_button.bgcolor = BUTTON_DISABLED_COLOR
                lock_button.disabled = True
                lock_button.bgcolor = BUTTON_DISABLED_COLOR
                unlock_unban_button.disabled = True
                unlock_unban_button.bgcolor = BUTTON_DISABLED_COLOR
            page.update()

        def generate_recovery_code():
            characters = string.ascii_uppercase + string.digits
            
            code = ''.join(secrets.choice(characters) for _ in range(12))
            
            code_field.text_field.value = code
            code_field.text_field.on_change(None)
            
            page.update()

        def handle_ban_action(data, reason, duration, days_data):

            if data is None:
                error_control = hashtag_field.error_text
                error_control.value = locale["moderation"]["errors"]["enter_valid_id"]
                error_control.visible = True

                page.update()
                return

            if not reason or not reason.strip():
                def close_dlg(e, dlg):
                    dlg.open = False
                    page.update()

                dlg = ft.AlertDialog(
                    title=ft.Text("🤬"),
                    content=ft.Text(locale["moderation"]["errors"]["enter_reason"]),
                    actions=[
                        ft.TextButton("OK", on_click=lambda e: page.close(dlg))
                    ],
                )

                page.open(dlg)

                page.update()
                return

            if duration == "custom_days":
                if days_data is None or days_data['days'] is None:
                    def close_dlg(e, dlg):
                        dlg.open = False
                        page.update()

                    dlg = ft.AlertDialog(
                        title=ft.Text("🤬"),
                        content=ft.Text(locale["moderation"]["errors"]["days_not_entered"]),
                        actions=[
                            ft.TextButton("OK", on_click=lambda e: page.close(dlg))
                        ],
                    )
                    page.open(dlg)

                    page.update()
                    return
                days = days_data['days']

            unban_timestamp = 0
            if duration == "custom_days":
                days = days_data['days']
                now = time.time()
                unban_timestamp = int(now + (days * 86400))

            if not user_client.connected:
                def close_dlg(e, dlg):
                    dlg.open = False
                    page.update()

                dlg = ft.AlertDialog(
                    title=ft.Text("🤬"),
                    content=ft.Text(locale["moderation"]["errors"]["connection_failed"]),
                    actions=[
                        ft.TextButton("OK", on_click=lambda e: page.close(dlg))
                    ],
                )
                page.open(dlg)

                page.update()
                return

            success, message = user_client.ban_user(data, reason, unban_timestamp)

            def close_dlg(e, dlg):
                dlg.open = False
                page.update()

            if success:
                dlg = ft.AlertDialog(
                    title=ft.Text("👌"),
                    content=ft.Text(message),
                    actions=[
                        ft.TextButton("OK", on_click=lambda e: close_dlg(e, dlg))
                    ],
                )

            else:
                dlg = ft.AlertDialog(
                    title=ft.Text("🤬"),
                    content=ft.Text(message),
                    actions=[
                        ft.TextButton("OK", on_click=lambda e: close_dlg(e, dlg))
                    ],
                )

            page.overlay.append(dlg)
            dlg.open = True
            page.update()

        def handle_lock_action(data):

            if data is None:
                error_control = hashtag_field.error_text
                error_control.value = locale["moderation"]["errors"]["enter_valid_id"]
                error_control.visible = True

                page.update()
                return

            code_data = code_field.get_current_data()
            if code_data is None or not code_data['code']:
                error_control = code_field.error_text
                error_control.value = locale["moderation"]["errors"]["enter_valid_code"]
                error_control.visible = True

                page.update()
                return
            if len(code_data['code']) != 12:
                error_control = code_field.error_text
                error_control.value = locale["moderation"]["errors"]["code_length_error"]
                error_control.visible = True

                page.update()
                return

            if not user_client.connected:
                def close_dlg(e, dlg):
                    dlg.open = False
                    page.update()

                dlg = ft.AlertDialog(
                    title=ft.Text("🤬"),
                    content=ft.Text(locale["moderation"]["errors"]["connection_failed"]),
                    actions=[
                        ft.TextButton("OK", on_click=lambda e: page.close(dlg))
                    ],
                )
                page.open(dlg)

                page.update()
                return

            recovery_code = code_data['code']
            success, message, returned_code = user_client.lock_user_account(data, recovery_code)

            if success:
                dlg = ft.AlertDialog(
                    title=ft.Text("👌"),
                    content=ft.Text(locale["moderation"]["success"]["account_locked"].format(returned_code)),
                    actions=[
                        ft.TextButton("OK", on_click=lambda e: page.close(dlg))
                    ],
                )

            else:
                dlg = ft.AlertDialog(
                    title=ft.Text("🤬"),
                    content=ft.Text(message),
                    actions=[
                        ft.TextButton("OK", on_click=lambda e: page.close(dlg))
                    ],
                )

            page.open(dlg)

            page.update()

        def handle_unlock_unban_action(data):

            if data is None:
                error_control = hashtag_field.error_text
                error_control.value = locale["moderation"]["errors"]["enter_valid_id"]
                error_control.visible = True

                page.update()
                return

            if not user_client.connected:
                def close_dlg(e, dlg):
                    dlg.open = False
                    page.update()

                dlg = ft.AlertDialog(
                    title=ft.Text("🤬"),
                    content=ft.Text(locale["moderation"]["errors"]["connection_failed"]),
                    actions=[
                        ft.TextButton("OK", on_click=lambda e: page.close(dlg))
                    ],
                )
                page.open(dlg)

                page.update()
                return

            success, message = user_client.unlock_user_account(data)

            if success:
                dlg = ft.AlertDialog(
                    title=ft.Text("👌"),
                    content=ft.Text(message),
                    actions=[
                        ft.TextButton("OK", on_click=lambda e: page.close(dlg))
                    ],
                )

            else:
                dlg = ft.AlertDialog(
                    title=ft.Text("🤬"),
                    content=ft.Text(message),
                    actions=[
                        ft.TextButton("OK", on_click=lambda e: page.close(dlg))
                    ],
                )

            page.open(dlg)
            page.update()

        def on_duration_change(e):
            if ban_duration_dropdown.value == "custom_days":
                days_field.visible = True
            else:
                days_field.visible = False
            page.update()

        ban_duration_dropdown.on_change = on_duration_change
        days_field.visible = False

        hashtag_field = hashtag_factory.create_hashtag_field(
            label=locale["moderation"]["user_id_label"],
            hint_text=locale["moderation"]["user_id_hint"],
            on_valid_change=update_button_states
        )

        table_rows = []
        for row_data in table_rows:
            action_text = locale["moderation"]["actions"][row_data["action"]]
            status_text = locale["moderation"]["statuses"][row_data["status"]]

            action_colors = {
                "ban": ERROR_COLOR,
                "lock": ERROR_COLOR,
                "unlock_unban": SUCCESS_COLOR,
                "warning": WARNING_COLOR,
                "verification": PRIMARY_COLOR
            }
            status_colors = {
                "active": SUCCESS_COLOR,
                "locked": ERROR_COLOR,
                "banned": ERROR_COLOR,
                "checking": WARNING_COLOR
            }
            action_color = action_colors.get(row_data["action"], PRIMARY_COLOR)
            status_color = status_colors.get(row_data["status"], SUCCESS_COLOR)
            table_rows.append(
                ft.DataRow(
                    cells=[
                        ft.DataCell(ft.Text(row_data["id"], color=TEXT_PRIMARY, size=14)),
                        ft.DataCell(ft.Text(row_data["user"], color=TEXT_PRIMARY, size=14)),
                        ft.DataCell(ft.Container(
                            content=ft.Text(action_text, size=12, color="white"),
                            bgcolor=action_color,
                            padding=ft.Padding(8, 4, 8, 4),
                            border_radius=6
                        )),
                        ft.DataCell(ft.Text(row_data["datetime"], color=TEXT_SECONDARY, size=13)),
                        ft.DataCell(ft.Text(status_text, color=status_color, size=14)),
                    ]
                )
            )
        current_view.content = ft.Column([
            ft.Row([
                ft.Text(locale["moderation"]["title"], size=32, weight=ft.FontWeight.BOLD, color=TEXT_PRIMARY),
                ft.Container(
                    content=ft.Text(locale["moderation"]["live_status"], size=12, color=SUCCESS_COLOR,
                                    weight=ft.FontWeight.BOLD),
                    bgcolor="#1b5e20",
                    padding=ft.Padding(8, 4, 8, 4),
                    border_radius=12
                )
            ], alignment=ft.MainAxisAlignment.START, spacing=15),
            ft.Text(locale["moderation"]["subtitle"], size=16, color=TEXT_SECONDARY),
            ft.Divider(height=40, color="transparent"),

            ft.Card(
                content=ft.Container(
                    content=ft.Column([
                        ft.Row([
                            ft.Icon("security", color=PRIMARY_COLOR, size=24),
                            ft.Text(locale["moderation"]["actions_card_title"], size=22, weight=ft.FontWeight.W_600,
                                    color=TEXT_PRIMARY),
                        ], alignment=ft.MainAxisAlignment.START, spacing=12),
                        ft.Divider(height=25, color="transparent"),
                        hashtag_field,
                        ft.Divider(height=15, color="transparent"),
                        ft.Text(locale["moderation"]["ban_button"], size=18, weight=ft.FontWeight.W_500, color=TEXT_PRIMARY),
                        reason_field,
                        ban_duration_dropdown,
                        days_field,
                        ft.Divider(height=15, color="transparent"),
                        ft.Text(locale["moderation"]["lock_button"], size=18, weight=ft.FontWeight.W_500, color=TEXT_PRIMARY),
                        code_field,
                        generate_code_button,
                        ft.Divider(height=15, color="transparent"),
                        ft.Row([
                            ban_button,
                            lock_button,
                            unlock_unban_button
                        ], spacing=15, wrap=True)
                    ], spacing=10),
                    padding=CARD_PADDING,
                    border_radius=CARD_BORDER_RADIUS
                ),
                elevation=8,
                color=SURFACE_COLOR
            ),
            ft.Divider(height=35, color="transparent")
        ], spacing=10, scroll=ft.ScrollMode.AUTO)
        page.update()

    def show_messages():
        hashtag_factory = HashtagFieldFactory(page, locale, styles)

        player_id_field = hashtag_factory.create_hashtag_field(
            label=locale["messages"]["player_id_label"],
            hint_text=locale["messages"]["player_id_hint"]
        )

        message_fields_container = ft.Column([])
        message_fields_list = []

        send_messages_button = ft.ElevatedButton(
            content=ft.Row([
                ft.Icon("send", size=18, color="white"),
                ft.Text(locale["messages"]["send_messages_button"], size=16, color="white")
            ], spacing=8, alignment=ft.CrossAxisAlignment.CENTER),
            bgcolor=BUTTON_DISABLED_COLOR,
            style=button_style,
            disabled=True,
        )

        add_message_field_button = ft.ElevatedButton(
            content=ft.Row([
                ft.Icon("add", size=18, color="white"),
                ft.Text(locale["messages"]["add_message_button"], size=16, color="white")
            ], spacing=8, alignment=ft.CrossAxisAlignment.CENTER),
            bgcolor=INFO_COLOR,
            style=button_style,
            on_click=lambda _: create_message_field()
        )

        messages_error_text = ft.Text(size=12, color=styles["colors"]["error"], visible=False)

        def is_messages_valid():
            player_data = player_id_field.get_current_data()
            if player_data is None:
                return False

            if len(message_fields_list) == 0:
                return False

            for item in message_fields_list:
                message_text = item["text_field"].value
                if not message_text or not message_text.strip():
                    return False
                stripped_text = message_text.strip()
                if len(stripped_text) < 3 or len(stripped_text) > 128:
                    return False
            return True

        def update_send_messages_button_state():
            if is_messages_valid():
                send_messages_button.disabled = False
                send_messages_button.bgcolor = SUCCESS_COLOR
            else:
                send_messages_button.disabled = True
                send_messages_button.bgcolor = BUTTON_DISABLED_COLOR
            page.update()

        def update_add_message_field_button_state():
            if len(message_fields_list) >= 3:
                add_message_field_button.disabled = True
                add_message_field_button.bgcolor = BUTTON_DISABLED_COLOR
            else:
                add_message_field_button.disabled = False
                add_message_field_button.bgcolor = INFO_COLOR
            page.update()

        def create_message_field():
            if len(message_fields_list) >= 3:
                return
            item_id = len(message_fields_list)

            is_system_message = ft.Checkbox(
                label=locale.get("messages", {}).get("system_message_checkbox", "System"),
                value=False,
            )

            message_text_field = ft.TextField(
                label=f"{locale['messages']['message_label']} {item_id + 1}",
                hint_text=locale["messages"]["message_hint"],
                multiline=True,
                min_lines=2,
                max_lines=5,
                width=400,
                border_radius=8,
                border_color=styles["colors"]["divider"],
                focused_border_color=styles["colors"]["primary"],
            )

            remove_message_button = ft.IconButton(
                icon="delete",
                icon_color=ERROR_COLOR if len(message_fields_list) > 0 else BUTTON_DISABLED_COLOR,
                disabled=len(message_fields_list) <= 1,
                on_click=lambda _: remove_message_field(item_id)
            )

            def update_remove_button_state():
                if len(message_fields_list) <= 1:
                    remove_message_button.disabled = True
                    remove_message_button.icon_color = BUTTON_DISABLED_COLOR
                else:
                    remove_message_button.disabled = False
                    remove_message_button.icon_color = ERROR_COLOR
                page.update()

            def on_message_text_change(e):
                update_send_messages_button_state()
                update_remove_button_state()

            def on_system_message_change(e):
                update_send_messages_button_state()

            message_text_field.on_change = on_message_text_change
            is_system_message.on_change = on_system_message_change

            message_input_column = ft.Column([
                message_text_field,
                ft.Row([
                    is_system_message,
                ], spacing=10)
            ])

            message_row = ft.Row([
                message_input_column,
                remove_message_button
            ], spacing=10, alignment=ft.MainAxisAlignment.START)

            item_data = {
                "id": item_id,
                "row": message_row,
                "text_field": message_text_field,
                "is_system_checkbox": is_system_message,
                "remove_button": remove_message_button
            }
            message_fields_list.append(item_data)
            message_fields_container.controls.append(message_row)

            update_add_message_field_button_state()
            update_send_messages_button_state()
            update_remove_button_state()
            page.update()

        def remove_message_field(item_id):
            if len(message_fields_list) <= 1:
                return

            item_to_remove = None
            for i, item in enumerate(message_fields_list):
                if item["id"] == item_id:
                    item_to_remove = item

                    for j in range(i + 1, len(message_fields_list)):
                        message_fields_list[j]["id"] = j - 1
                        message_fields_list[j]["text_field"].label = f"{locale['messages']['message_label']} {j}"
                        message_fields_list[j]["remove_button"].on_click = lambda _, id=j - 1: remove_message_field(id)
                    break
            if item_to_remove:
                message_fields_list.remove(item_to_remove)
                message_fields_container.controls.remove(item_to_remove["row"])

                for idx, item in enumerate(message_fields_list):
                    item["text_field"].label = f"{locale['messages']['message_label']} {idx + 1}"

                update_add_message_field_button_state()
                update_send_messages_button_state()

                for item in message_fields_list:
                    if len(message_fields_list) <= 1:
                        item["remove_button"].disabled = True
                        item["remove_button"].icon_color = BUTTON_DISABLED_COLOR
                    else:
                        item["remove_button"].disabled = False
                        item["remove_button"].icon_color = ERROR_COLOR
                page.update()

        def handle_send_messages(e):

            if not is_messages_valid():
                page.update()
                return

            player_data = player_id_field.get_current_data()
            if player_data is None:
                messages_error_text.value = locale["messages"]["errors"]["enter_valid_player_id"]
                messages_error_text.color = styles["colors"]["error"]
                messages_error_text.visible = True
                page.update()
                return

            messages_info = []
            validation_failed = False

            for i, item in enumerate(message_fields_list):
                message_text = item["text_field"].value or ""
                stripped_text = message_text.strip()

                is_system = item.get("is_system_checkbox", ft.Checkbox(value=False)).value

                if len(stripped_text) < 3 or len(stripped_text) > 128:
                    messages_error_text.value = f"{locale['messages']['errors']['message_length_error']} ({i + 1})"
                    messages_error_text.color = styles["colors"]["error"]
                    messages_error_text.visible = True
                    validation_failed = True
                    break

                messages_info.append({
                    "text": stripped_text,
                    "is_system": is_system
                })

            if validation_failed:
                page.update()
                return

            messages_client = MessagesClient()
            connected = messages_client.connect()

            if not connected:
                messages_error_text.value = "Error: not connected"
                messages_error_text.color = styles["colors"]["error"]
                messages_error_text.visible = True
                page.update()
                return

            try:
                success, message = messages_client.send_messages(player_data, messages_info)

                if success:

                    message_fields_container.controls.clear()
                    message_fields_list.clear()
                    create_message_field()
                    update_add_message_field_button_state()
                    update_send_messages_button_state()

                    messages_error_text.value = message
                    messages_error_text.color = styles["colors"].get("success", "green")
                    messages_error_text.visible = True

                    dlg = ft.AlertDialog(
                        title=ft.Text("👌"),
                        content=ft.Text(message),
                        actions=[
                            ft.TextButton("OK", on_click=lambda e: page.close(dlg))
                        ],
                    )
                    page.open(dlg)
                else:

                    messages_error_text.value = message
                    messages_error_text.color = styles["colors"]["error"]
                    messages_error_text.visible = True

                    dlg = ft.AlertDialog(
                        title=ft.Text("🤬"),
                        content=ft.Text(message),
                        actions=[
                            ft.TextButton("OK", on_click=lambda e: page.close(dlg))
                        ],
                    )
                    page.open(dlg)
            finally:
                messages_client.close()

            page.update()

        send_messages_button.on_click = handle_send_messages

        player_id_field = hashtag_factory.create_hashtag_field(
            label=locale["messages"]["player_id_label"],
            hint_text=locale["messages"]["player_id_hint"],
            on_valid_change=lambda valid_data: update_send_messages_button_state()
        )

        create_message_field()

        current_view.content = ft.Column([
            ft.Row([
                ft.Text(locale["messages"]["title"], size=32, weight=ft.FontWeight.BOLD, color=TEXT_PRIMARY),
                ft.Container(
                    content=ft.Text(locale["messages"]["count"], size=12, color=TEXT_SECONDARY,
                                    weight=ft.FontWeight.BOLD),
                    bgcolor="#333333",
                    padding=ft.Padding(8, 4, 8, 4),
                    border_radius=12
                )
            ], alignment=ft.MainAxisAlignment.START, spacing=15),
            ft.Text(locale["messages"]["subtitle"], size=16, color=TEXT_SECONDARY),
            ft.Divider(height=40, color="transparent"),
            ft.Card(
                content=ft.Container(
                    content=ft.Column([
                        ft.Row([
                            ft.Icon("forum", color=PRIMARY_COLOR, size=24),
                            ft.Text(locale["messages"]["send_messages_title"], size=22, weight=ft.FontWeight.W_600,
                                    color=TEXT_PRIMARY),
                        ], alignment=ft.MainAxisAlignment.START, spacing=12),
                        ft.Divider(height=25, color="transparent"),
                        player_id_field,
                        ft.Divider(height=15, color="transparent"),
                        ft.Text(locale["messages"]["messages_title"], size=18, weight=ft.FontWeight.W_500,
                                color=TEXT_PRIMARY),
                        add_message_field_button,
                        message_fields_container,
                        messages_error_text,
                        ft.Divider(height=15, color="transparent"),
                        ft.Row([
                            send_messages_button,
                        ], spacing=15)
                    ], spacing=10),
                    padding=CARD_PADDING,
                    border_radius=CARD_BORDER_RADIUS
                ),
                elevation=8,
                color=SURFACE_COLOR
            )
        ], scroll=ft.ScrollMode.AUTO)

        page.update()

    def show_gifts():
        hashtag_factory = HashtagFieldFactory(page, locale, styles)

        player_id_field = hashtag_factory.create_hashtag_field(
            label=locale["gifts"]["player_id_label"],
            hint_text=locale["gifts"]["player_id_hint"]
        )

        gift_items_container = ft.Column([])
        gift_items_list = []

        send_gift_button = ft.ElevatedButton(
            content=ft.Row([
                ft.Icon("send", size=18, color="white"),
                ft.Text(locale["gifts"]["send_gift_button"], size=16, color="white")
            ], spacing=8, alignment=ft.CrossAxisAlignment.CENTER),
            bgcolor=BUTTON_DISABLED_COLOR,
            style=button_style,
            disabled=True,
        )

        add_gift_item_button = ft.ElevatedButton(
            content=ft.Row([
                ft.Icon("add", size=18, color="white"),
                ft.Text(locale["gifts"]["add_item_button"], size=16, color="white")
            ], spacing=8, alignment=ft.CrossAxisAlignment.CENTER),
            bgcolor=INFO_COLOR,
            style=button_style,
            on_click=lambda _: create_gift_item_field()
        )

        gift_error_text = ft.Text(size=12, color=styles["colors"]["error"], visible=False)

        def is_gift_valid():
            player_data = player_id_field.get_current_data()
            if player_data is None:
                return False

            if len(gift_items_list) == 0:
                return False

            for item in gift_items_list:
                item_type = item["type_dropdown"].value
                if not item_type:
                    return False

                if item_type == "3":  # Brawler
                    if not item["fighter_name_field"].value or not item["fighter_name_field"].value.strip():
                        return False
                elif item_type == "4":  # Skin
                    if not item["skin_name_field"].value or not item["skin_name_field"].value.strip():
                        return False

                else:
                    if not item["quantity_field"].value or not item["quantity_field"].value.strip():
                        return False
                    try:
                        qty = int(item["quantity_field"].value)
                        if qty < 1 or qty > 99999:
                            return False
                    except ValueError:
                        return False
            return True

        def update_send_gift_button_state():
            if is_gift_valid():
                send_gift_button.disabled = False
                send_gift_button.bgcolor = SUCCESS_COLOR
            else:
                send_gift_button.disabled = True
                send_gift_button.bgcolor = BUTTON_DISABLED_COLOR
            page.update()

        def update_add_gift_item_button_state():
            if len(gift_items_list) >= 5:
                add_gift_item_button.disabled = True
                add_gift_item_button.bgcolor = BUTTON_DISABLED_COLOR
            else:
                add_gift_item_button.disabled = False
                add_gift_item_button.bgcolor = INFO_COLOR
            page.update()

        def create_gift_item_field():
            if len(gift_items_list) >= 5:
                return

            item_id = len(gift_items_list)

            item_type_dropdown = ft.Dropdown(
                label=locale["gifts"]["item_type_label"],
                width=200,
                border_radius=8,
                border_color=styles["colors"]["divider"],
                focused_border_color=styles["colors"]["primary"],
                options=[
                    ft.dropdown.Option("1", locale["shop"]["item_types"]["1"]),  # Gold
                    ft.dropdown.Option("16", locale["shop"]["item_types"]["16"]),  # Diamonds
                    ft.dropdown.Option("7", locale["shop"]["item_types"]["7"]),  # Tickets
                    ft.dropdown.Option("9", locale["shop"]["item_types"]["9"]),  # Token Doubler
                    ft.dropdown.Option("17", locale["shop"]["item_types"]["17"]),  # Star Points
                    ft.dropdown.Option("3", locale["shop"]["item_types"]["3"]),  # Brawler
                    ft.dropdown.Option("4", locale["shop"]["item_types"]["4"]),  # Brawler Skin
                ]
            )

            quantity_field = ft.TextField(
                label=locale["gifts"]["item_quantity_label"],
                hint_text=locale["gifts"]["item_quantity_hint"],
                width=150,
                border_radius=8,
                border_color=styles["colors"]["divider"],
                focused_border_color=styles["colors"]["primary"],
                keyboard_type=ft.KeyboardType.NUMBER,
                visible=True
            )

            fighter_name_field = ft.TextField(
                label=locale["gifts"]["fighter_name_label"],
                hint_text=locale["gifts"]["fighter_name_hint"],
                width=200,
                border_radius=8,
                border_color=styles["colors"]["divider"],
                focused_border_color=styles["colors"]["primary"],
                visible=False
            )

            skin_name_field = ft.TextField(
                label=locale["gifts"]["skin_name_label"],
                hint_text=locale["gifts"]["skin_name_hint"],
                width=200,
                border_radius=8,
                border_color=styles["colors"]["divider"],
                focused_border_color=styles["colors"]["primary"],
                visible=False
            )

            remove_item_button = ft.IconButton(
                icon="delete",
                icon_color=ERROR_COLOR,
                on_click=lambda _: remove_gift_item(item_id)
            )

            def on_item_type_change(e):
                selected_type = item_type_dropdown.value
                if selected_type == "3":  # Brawler
                    fighter_name_field.visible = True
                    skin_name_field.visible = False
                    quantity_field.visible = False
                elif selected_type == "4":  # Skin
                    fighter_name_field.visible = False
                    skin_name_field.visible = True
                    quantity_field.visible = False
                else:
                    fighter_name_field.visible = False
                    skin_name_field.visible = False
                    quantity_field.visible = True
                update_send_gift_button_state()
                page.update()

            item_type_dropdown.on_change = on_item_type_change

            def on_quantity_change(e):
                update_send_gift_button_state()

            def on_fighter_name_change(e):
                update_send_gift_button_state()

            def on_skin_name_change(e):
                update_send_gift_button_state()

            quantity_field.on_change = on_quantity_change
            fighter_name_field.on_change = on_fighter_name_change
            skin_name_field.on_change = on_skin_name_change

            item_row = ft.Row([
                item_type_dropdown,
                quantity_field,
                fighter_name_field,
                skin_name_field,
                remove_item_button
            ], spacing=10, alignment=ft.MainAxisAlignment.START)

            item_data = {
                "id": item_id,
                "row": item_row,
                "type_dropdown": item_type_dropdown,
                "quantity_field": quantity_field,
                "fighter_name_field": fighter_name_field,
                "skin_name_field": skin_name_field,
                "remove_button": remove_item_button
            }

            gift_items_list.append(item_data)
            gift_items_container.controls.append(item_row)

            update_add_gift_item_button_state()
            update_send_gift_button_state()
            page.update()

        def remove_gift_item(item_id):
            if len(gift_items_list) <= 1:
                return

            item_to_remove = None
            for i, item in enumerate(gift_items_list):
                if item["id"] == item_id:
                    item_to_remove = item
                    for j in range(i + 1, len(gift_items_list)):
                        gift_items_list[j]["id"] = j - 1
                        gift_items_list[j]["remove_button"].on_click = lambda _, id=j - 1: remove_gift_item(id)
                    break

            if item_to_remove:
                gift_items_list.remove(item_to_remove)
                gift_items_container.controls.remove(item_to_remove["row"])

                update_add_gift_item_button_state()
                update_send_gift_button_state()
                page.update()

        def handle_send_gift(e):

            if not is_gift_valid():
                page.update()
                return

            player_data = player_id_field.get_current_data()
            if player_data is None:
                gift_error_text.value = locale["gifts"]["errors"][
                    "enter_valid_player_id"]
                gift_error_text.color = styles["colors"]["error"]
                gift_error_text.visible = True
                page.update()
                return

            items_info = []
            validation_failed = False

            for i, item in enumerate(gift_items_list):
                item_type = int(item["type_dropdown"].value) if item["type_dropdown"].value else -1
                if item_type == -1:
                    gift_error_text.value = f"{locale['gifts']['errors']['enter_item_type']} ({i + 1})"
                    gift_error_text.color = styles["colors"]["error"]
                    gift_error_text.visible = True
                    validation_failed = True
                    break

                item_data = {"type": item_type}

                if item_type not in [3, 4]:
                    quantity_str = item["quantity_field"].value or ""
                    if not quantity_str:
                        gift_error_text.value = f"{locale['gifts']['errors']['enter_item_quantity']} ({i + 1})"
                        gift_error_text.color = styles["colors"]["error"]
                        gift_error_text.visible = True
                        validation_failed = True
                        break
                    try:
                        item_data["quantity"] = int(quantity_str)
                        if item_data["quantity"] < 1 or item_data["quantity"] > 99999:
                            raise ValueError("Out of range")
                    except (ValueError, TypeError):
                        gift_error_text.value = f"{locale['gifts']['errors']['enter_item_quantity']} ({i + 1})"
                        gift_error_text.color = styles["colors"]["error"]
                        gift_error_text.visible = True
                        validation_failed = True
                        break

                if item_type == 3:  # Brawler
                    brawler_name = item["fighter_name_field"].value or ""
                    if not brawler_name.strip():
                        gift_error_text.value = f"{locale['gifts']['errors']['enter_fighter_name']} ({i + 1})"
                        gift_error_text.color = styles["colors"]["error"]
                        gift_error_text.visible = True
                        validation_failed = True
                        break
                    item_data["brawler_name"] = brawler_name.strip()
                elif item_type == 4:  # Skin
                    skin_name = item["skin_name_field"].value or ""
                    if not skin_name.strip():
                        gift_error_text.value = f"{locale['gifts']['errors']['enter_skin_name']} ({i + 1})"
                        gift_error_text.color = styles["colors"]["error"]
                        gift_error_text.visible = True
                        validation_failed = True
                        break
                    item_data["skin_name"] = skin_name.strip()

                items_info.append(item_data)

            if validation_failed:
                page.update()
                return

            if not connected:
                gift_error_text.value = "Error: not connected"
                gift_error_text.color = styles["colors"]["error"]
                gift_error_text.visible = True
                page.update()
                return

            try:
                success, message = gifts_client.send_gift(player_data, items_info, reason="Admin gift")

                if success:

                    gift_items_container.controls.clear()
                    gift_items_list.clear()
                    create_gift_item_field()
                    update_add_gift_item_button_state()
                    update_send_gift_button_state()

                    gift_error_text.value = message
                    gift_error_text.color = styles["colors"][
                        "success"]
                    gift_error_text.visible = True
                else:

                    gift_error_text.value = message
                    gift_error_text.color = styles["colors"]["error"]
                    gift_error_text.visible = True
            finally:
                pass

            page.update()

        send_gift_button.on_click = handle_send_gift

        player_id_field = hashtag_factory.create_hashtag_field(
            label=locale["gifts"]["player_id_label"],
            hint_text=locale["gifts"]["player_id_hint"],
            on_valid_change=lambda valid_data: update_send_gift_button_state()
        )

        create_gift_item_field()

        current_view.content = ft.Column([
            ft.Row([
                ft.Text(locale["gifts"]["title"], size=32, weight=ft.FontWeight.BOLD, color=TEXT_PRIMARY),
                ft.Container(
                    content=ft.Text(locale["gifts"]["count"], size=12, color=TEXT_SECONDARY, weight=ft.FontWeight.BOLD),
                    bgcolor="#333333",
                    padding=ft.Padding(8, 4, 8, 4),
                    border_radius=12
                )
            ], alignment=ft.MainAxisAlignment.START, spacing=15),
            ft.Text(locale["gifts"]["subtitle"], size=16, color=TEXT_SECONDARY),
            ft.Divider(height=40, color="transparent"),
            ft.Card(
                content=ft.Container(
                    content=ft.Column([
                        ft.Row([
                            ft.Icon("card_giftcard", color=PRIMARY_COLOR, size=24),
                            ft.Text(locale["gifts"]["send_gift_title"], size=22, weight=ft.FontWeight.W_600,
                                    color=TEXT_PRIMARY),
                        ], alignment=ft.MainAxisAlignment.START, spacing=12),
                        ft.Divider(height=25, color="transparent"),
                        player_id_field,
                        ft.Divider(height=15, color="transparent"),
                        ft.Text(locale["gifts"]["items_title"], size=18, weight=ft.FontWeight.W_500,
                                color=TEXT_PRIMARY),
                        add_gift_item_button,
                        gift_items_container,
                        gift_error_text,
                        ft.Divider(height=15, color="transparent"),
                        ft.Row([
                            send_gift_button,
                        ], spacing=15)
                    ], spacing=10),
                    padding=CARD_PADDING,
                    border_radius=CARD_BORDER_RADIUS
                ),
                elevation=8,
                color=SURFACE_COLOR
            )
        ], scroll=ft.ScrollMode.AUTO)

        page.update()

    def show_shop():
        current_shop_view = ft.Column([])

        def show_shop_main():
            create_promotion_button = ft.ElevatedButton(
                content=ft.Row([
                    ft.Icon("add", size=18, color="white"),
                    ft.Text(locale["shop"]["create_promotion"], size=16, color="white")
                ], spacing=8, alignment=ft.CrossAxisAlignment.CENTER),
                bgcolor=PRIMARY_COLOR,
                style=button_style,
                on_click=lambda _: show_create_promotion()
            )

            delete_promotion_button = ft.ElevatedButton(
                content=ft.Row([
                    ft.Icon("delete", size=18, color="white"),
                    ft.Text(locale["shop"]["delete_promotion"], size=16, color="white")
                ], spacing=8, alignment=ft.CrossAxisAlignment.CENTER),
                bgcolor=ERROR_COLOR,
                style=button_style,
                on_click=lambda _: show_delete_promotion()
            )

            promotion_history_button = ft.ElevatedButton(
                content=ft.Row([
                    ft.Icon("history", size=18, color="white"),
                    ft.Text(locale["shop"]["promotion_history"], size=16, color="white")
                ], spacing=8, alignment=ft.CrossAxisAlignment.CENTER),
                bgcolor=INFO_COLOR,
                style=button_style,
                on_click=lambda _: show_promotion_history()
            )

            current_shop_view.controls = [
                ft.Row([
                    ft.Text(locale["shop"]["title"], size=32, weight=ft.FontWeight.BOLD, color=TEXT_PRIMARY),
                    ft.Container(
                        content=ft.Text(locale["shop"]["count"], size=12, color=TEXT_SECONDARY,
                                        weight=ft.FontWeight.BOLD),
                        bgcolor="#333333",
                        padding=ft.Padding(8, 4, 8, 4),
                        border_radius=12
                    )
                ], alignment=ft.MainAxisAlignment.START, spacing=15),
                ft.Text(locale["shop"]["subtitle"], size=16, color=TEXT_SECONDARY),
                ft.Divider(height=50, color="transparent"),
                ft.Card(
                    content=ft.Container(
                        content=ft.Column([
                            ft.Icon("shopping_cart", size=48, color=TEXT_SECONDARY),
                            ft.Text(locale["shop"]["card_title"], size=20, weight=ft.FontWeight.W_500,
                                    color=TEXT_PRIMARY),
                            ft.Divider(height=25, color="transparent"),
                            ft.Row([
                                create_promotion_button,
                                delete_promotion_button,
                                promotion_history_button
                            ], spacing=15, wrap=True)
                        ],
                            horizontal_alignment=ft.CrossAxisAlignment.CENTER,
                            spacing=10),
                        padding=50,
                        border_radius=CARD_BORDER_RADIUS
                    ),
                    elevation=8,
                    color=SURFACE_COLOR
                )
            ]
            page.update()

        def show_delete_promotion():

            promotion_id_field = ft.TextField(
                label=locale["shop"]["promotion_id_label"],
                hint_text=locale["shop"]["promotion_id_hint"],
                width=320,
                border_radius=8,
                border_color=styles["colors"]["divider"],
                focused_border_color=styles["colors"]["primary"],
                prefix_icon="tag"
            )

            error_text = ft.Text(size=12, color=styles["colors"]["error"], visible=False)

            def handle_delete(e):

                promotion_id = promotion_id_field.value.strip()

                if not promotion_id:
                    error_text.value = locale["shop"]["errors"]["enter_promotion_id"]
                    error_text.visible = True
                    page.update()
                    return

                if not shop_client.connected:
                    error_text.value = locale["shop"]["errors"]["grpc_connection_failed"]
                    error_text.visible = True
                    page.update()
                    return

                try:

                    success, message = shop_client.delete_promotion(promotion_id)

                    error_text.visible = False
                    error_text.value = ""

                    if success:

                        error_text.value = message
                        error_text.color = styles["colors"]["success"]
                        promotion_id_field.value = ""
                    else:

                        error_text.value = message
                        error_text.color = styles["colors"]["error"]

                    error_text.visible = True
                    page.update()

                except Exception as ex:
                    error_text.value = f"Critical Error: {ex}"
                    error_text.color = styles["colors"]["error"]
                    error_text.visible = True
                    page.update()

            delete_button = ft.ElevatedButton(
                content=ft.Row([
                    ft.Icon("delete", size=18, color="white"),
                    ft.Text(locale["shop"]["delete_button"], size=16, color="white")
                ], spacing=8, alignment=ft.CrossAxisAlignment.CENTER),
                bgcolor=ERROR_COLOR,
                style=button_style,
                on_click=handle_delete
            )

            back_button = ft.ElevatedButton(
                content=ft.Row([
                    ft.Icon("arrow_back", size=18, color="white"),
                    ft.Text(locale["shop"]["back_button"], size=16, color="white")
                ], spacing=8, alignment=ft.CrossAxisAlignment.CENTER),
                bgcolor=BUTTON_DISABLED_COLOR,
                style=button_style,
                on_click=lambda _: show_shop_main()
            )

            current_shop_view.controls = [
                ft.Card(
                    content=ft.Container(
                        content=ft.Column([
                            ft.Row([
                                ft.Icon("delete", color=ERROR_COLOR, size=24),
                                ft.Text(locale["shop"]["delete_promotion_title"], size=22, weight=ft.FontWeight.W_600,
                                        color=TEXT_PRIMARY),
                            ], alignment=ft.MainAxisAlignment.START, spacing=12),
                            ft.Divider(height=25, color="transparent"),
                            promotion_id_field,
                            error_text,
                            ft.Divider(height=15, color="transparent"),
                            ft.Row([delete_button, back_button], spacing=15),
                        ], spacing=10),
                        padding=CARD_PADDING,
                        border_radius=CARD_BORDER_RADIUS
                    ),
                    elevation=8,
                    color=SURFACE_COLOR
                )
            ]
            page.update()

        def copy_to_clipboard(text, e=None):
            try:
                page.set_clipboard(text)

                r = locale["shop"]["copied"];

                snack_bar = ft.SnackBar(
                    content=ft.Text(f"ID '{text}' {r}", size=14, color="white"),
                    bgcolor=PRIMARY_COLOR,
                    duration=2000
                )

                page.snack_bar = snack_bar
                page.snack_bar.open = True
                page.open(snack_bar)

                page.update()
            except Exception as ex:
                display_shop_error(f"{str(ex)}")

        def show_promotion_history():

            if not shop_client.connected:
                display_shop_error(locale["shop"]["errors"]["grpc_connection_failed"])
                return

            try:
                success, message, promotions_data = shop_client.get_promotion_history()

                back_button = ft.ElevatedButton(
                    content=ft.Row([
                        ft.Icon("arrow_back", size=18, color="white"),
                        ft.Text(locale["shop"]["back_button"], size=16, color="white")
                    ], spacing=8, alignment=ft.CrossAxisAlignment.CENTER),
                    bgcolor=BUTTON_DISABLED_COLOR,
                    style=button_style,
                    on_click=lambda _: show_shop_main()
                )

                table_rows = []
                if success and promotions_data:
                    for promo in promotions_data:
                        promo_id = str(promo.get("id", "N/A"))

                        id_cell = ft.DataCell(
                            ft.GestureDetector(
                                on_tap=lambda e, pid=promo_id: copy_to_clipboard(pid),
                                mouse_cursor=ft.MouseCursor.CLICK,
                                content=ft.Text(
                                    promo_id,
                                    color=PRIMARY_COLOR,
                                    size=14,
                                    style=ft.TextStyle(decoration=ft.TextDecoration.UNDERLINE)
                                ),
                            )
                        )

                        table_rows.append(
                            ft.DataRow(
                                cells=[
                                    id_cell,
                                    ft.DataCell(ft.Text(promo.get("name", "N/A"), color=TEXT_PRIMARY, size=14)),
                                ]
                            )
                        )

                table_content = (
                    ft.DataTable(
                        columns=[
                            ft.DataColumn(ft.Text("ID", size=14, weight=ft.FontWeight.W_500, color=TEXT_SECONDARY)),
                            ft.DataColumn(
                                ft.Text(locale["shop"]["promotion_name_label"], size=14, weight=ft.FontWeight.W_500,
                                        color=TEXT_SECONDARY)),
                        ],
                        rows=table_rows,
                        border_radius=12,
                        heading_row_color=SURFACE_COLOR,
                        data_row_min_height=55,
                        column_spacing=20,
                        divider_thickness=0.5,
                        border=ft.border.all(1, DIVIDER_COLOR)
                    ) if table_rows else ft.Text(locale["shop"]["no_promotions"], size=16, color=TEXT_SECONDARY)
                )

                card_content = ft.Column([
                    ft.Row([
                        ft.Icon("history", color=PRIMARY_COLOR, size=24),
                        ft.Text(locale["shop"]["promotion_history_title"], size=22, weight=ft.FontWeight.W_600,
                                color=TEXT_PRIMARY),
                    ], alignment=ft.MainAxisAlignment.START, spacing=12),
                    ft.Divider(height=25, color="transparent"),
                    table_content,
                    ft.Divider(height=25, color="transparent"),
                    back_button
                ], spacing=10)

                container_content = ft.Container(
                    content=card_content,
                    padding=CARD_PADDING,
                    border_radius=CARD_BORDER_RADIUS
                )

                history_card = ft.Card(
                    content=container_content,
                    elevation=8,
                    color=SURFACE_COLOR
                )

                current_shop_view.controls = [history_card]
                page.update()


            except grpc.RpcError as e:

                display_shop_error(f"gRPC error: {e.details()}")
            except Exception as e:

                display_shop_error(f"Error: {str(e)}")

        def display_shop_error(message):
            error_card = ft.Card(
                content=ft.Container(
                    content=ft.Column([
                        ft.Text(locale["shop"]["error_title"], size=20, weight=ft.FontWeight.BOLD, color=ERROR_COLOR),
                        ft.Text(message, size=16, color=TEXT_SECONDARY)
                    ], spacing=10),
                    padding=CARD_PADDING,
                    border_radius=CARD_BORDER_RADIUS
                ),
                elevation=8,
                color=SURFACE_COLOR
            )
            current_shop_view.controls = [error_card]
            page.update()

        success, message, promotions_data = shop_client.get_promotion_history()

        table_rows = []
        if success and promotions_data:
            for promo in promotions_data:
                table_rows.append(
                    ft.DataRow(
                        cells=[
                            ft.DataCell(ft.Text(str(promo.get("id", "N/A")), color=TEXT_PRIMARY, size=14)),
                            ft.DataCell(ft.Text(promo.get("name", "N/A"), color=TEXT_PRIMARY, size=14)),
                        ]
                    )
                )

        back_button = ft.ElevatedButton(
            content=ft.Row([
                ft.Icon("arrow_back", size=18, color="white"),
                ft.Text(locale["shop"]["cancel_button"], size=16, color="white")
            ], spacing=8, alignment=ft.CrossAxisAlignment.CENTER),
            bgcolor=BUTTON_DISABLED_COLOR,
            style=button_style,
            on_click=lambda _: show_shop_main()
        )

        current_shop_view.controls = [
            ft.Card(
                content=ft.Container(
                    content=ft.Column([
                        ft.Row([
                            ft.Icon("history", color=PRIMARY_COLOR, size=24),
                            ft.Text(locale["shop"]["promotion_history_title"], size=22, weight=ft.FontWeight.W_600,
                                    color=TEXT_PRIMARY),
                        ], alignment=ft.MainAxisAlignment.START, spacing=12),
                        ft.Divider(height=25, color="transparent"),

                        ft.DataTable(
                            columns=[
                                ft.DataColumn(ft.Text("ID", size=14, weight=ft.FontWeight.W_500, color=TEXT_SECONDARY)),
                                ft.DataColumn(
                                    ft.Text(locale["shop"]["promotion_name_label"], size=14, weight=ft.FontWeight.W_500,
                                            color=TEXT_SECONDARY)),
                            ],
                            rows=table_rows,
                            border_radius=12,
                            heading_row_color=SURFACE_COLOR,
                            data_row_min_height=55,
                            column_spacing=20,
                            divider_thickness=0.5,
                            border=ft.border.all(1, DIVIDER_COLOR)
                        ) if table_rows else ft.Text(locale["shop"]["no_promotions"], size=16, color=TEXT_SECONDARY),
                        ft.Divider(height=25, color="transparent"),
                        back_button
                    ], spacing=10),
                    padding=CARD_PADDING,
                    border_radius=CARD_BORDER_RADIUS
                ),
                elevation=8,
                color=SURFACE_COLOR
            )
        ]
        page.update()

        def show_create_promotion():
            promotion_name_field = ft.TextField(
                label=locale["shop"]["promotion_name_label"],
                hint_text=locale["shop"]["promotion_name_hint"],
                width=320,
                border_radius=8,
                border_color=styles["colors"]["divider"],
                focused_border_color=styles["colors"]["primary"],
                prefix_icon="title"
            )

            is_daily_checkbox = ft.Checkbox(
                label=locale["shop"]["is_daily_label"],
                value=False
            )

            start_time_field = ft.TextField(
                label=locale["shop"]["start_time_label"],
                hint_text=locale["shop"]["start_time_hint"],
                width=320,
                border_radius=8,
                border_color=styles["colors"]["divider"],
                focused_border_color=styles["colors"]["primary"],
                prefix_icon="schedule"
            )

            end_time_field = ft.TextField(
                label=locale["shop"]["end_time_label"],
                hint_text=locale["shop"]["end_time_hint"],
                width=320,
                border_radius=8,
                border_color=styles["colors"]["divider"],
                focused_border_color=styles["colors"]["primary"],
                prefix_icon="schedule"
            )

            price_field = ft.TextField(
                label=locale["shop"]["price_label"],
                hint_text=locale["shop"]["price_hint"],
                width=320,
                border_radius=8,
                border_color=styles["colors"]["divider"],
                focused_border_color=styles["colors"]["primary"],
                prefix_icon="payments"
            )

            old_price_field = ft.TextField(
                label=locale["shop"]["old_price_label"],
                hint_text=locale["shop"]["old_price_hint"],
                width=320,
                border_radius=8,
                border_color=styles["colors"]["divider"],
                focused_border_color=styles["colors"]["primary"],
                prefix_icon="local_offer"
            )

            price_type_dropdown = ft.Dropdown(
                label=locale["shop"]["price_type_label"],
                width=320,
                border_radius=8,
                border_color=styles["colors"]["divider"],
                focused_border_color=styles["colors"]["primary"],
                options=[
                    ft.dropdown.Option("0", locale["shop"]["price_types"]["0"]),
                    ft.dropdown.Option("1", locale["shop"]["price_types"]["1"]),
                    ft.dropdown.Option("2", locale["shop"]["price_types"]["2"]),
                    ft.dropdown.Option("3", locale["shop"]["price_types"]["3"]),
                    ft.dropdown.Option("4", locale["shop"]["price_types"]["4"]),
                ]
            )

            background_field = ft.TextField(
                label=locale["shop"]["background_label"],
                hint_text=locale["shop"]["background_hint"],
                width=320,
                border_radius=8,
                border_color=styles["colors"]["divider"],
                focused_border_color=styles["colors"]["primary"],
                prefix_icon="image"
            )

            show_to_new_users_checkbox = ft.Checkbox(
                label=locale["shop"]["show_to_new_users_label"],
                value=True
            )

            items_container = ft.Column([])
            items_list = []

            create_button = ft.ElevatedButton(
                content=ft.Row([
                    ft.Icon("check", size=18, color="white"),
                    ft.Text(locale["shop"]["create_button"], size=16, color="white")
                ], spacing=8, alignment=ft.CrossAxisAlignment.CENTER),
                bgcolor=BUTTON_DISABLED_COLOR,
                style=button_style,
                disabled=True,
                on_click=lambda e: handle_create_promotion(e)
            )

            add_item_button = ft.ElevatedButton(
                content=ft.Row([
                    ft.Icon("add", size=18, color="white"),
                    ft.Text(locale["shop"]["add_item_button"], size=16, color="white")
                ], spacing=8, alignment=ft.CrossAxisAlignment.CENTER),
                bgcolor=INFO_COLOR,
                style=button_style,
                on_click=lambda _: create_item_field()
            )

            def is_promotion_valid():
                if (not promotion_name_field.value or not promotion_name_field.value.strip() or
                        not start_time_field.value or not start_time_field.value.strip() or
                        not end_time_field.value or not end_time_field.value.strip() or
                        not price_field.value or not price_field.value.strip() or
                        not old_price_field.value or not old_price_field.value.strip() or
                        not price_type_dropdown.value):
                    return False

                try:
                    int(start_time_field.value)
                    int(end_time_field.value)
                    float(price_field.value)
                    float(old_price_field.value)
                except ValueError:
                    return False

                if len(items_list) == 0:
                    return False

                for item in items_list:
                    item_type = item["type_dropdown"].value
                    if not item_type:
                        return False

                    if item_type == "3":  # Brawler
                        if not item["fighter_name_field"].value or not item["fighter_name_field"].value.strip():
                            return False
                    elif item_type == "4":  # Skin
                        if not item["skin_name_field"].value or not item["skin_name_field"].value.strip():
                            return False
                    else:
                        if not item["quantity_field"].value or not item["quantity_field"].value.strip():
                            return False
                        try:
                            qty = int(item["quantity_field"].value)
                            if qty < 1 or qty > 99999:
                                return False
                        except ValueError:
                            return False

                return True

            def update_create_button_state():
                if is_promotion_valid():
                    create_button.disabled = False
                    create_button.bgcolor = SUCCESS_COLOR
                else:
                    create_button.disabled = True
                    create_button.bgcolor = BUTTON_DISABLED_COLOR
                page.update()

            def update_add_item_button_state():
                if len(items_list) >= 4:
                    add_item_button.disabled = True
                    add_item_button.bgcolor = BUTTON_DISABLED_COLOR
                else:
                    add_item_button.disabled = False
                    add_item_button.bgcolor = INFO_COLOR
                page.update()

            def update_remove_item_buttons_state():
                for item in items_list:
                    remove_button = item["remove_button"]
                    if len(items_list) <= 1:
                        remove_button.disabled = True
                        remove_button.icon_color = BUTTON_DISABLED_COLOR
                    else:
                        remove_button.disabled = False
                        remove_button.icon_color = ERROR_COLOR
                page.update()

            def create_item_field():
                if len(items_list) >= 4:
                    return

                item_id = len(items_list)

                item_type_dropdown = ft.Dropdown(
                    label=locale["shop"]["item_type_label"],
                    width=200,
                    border_radius=8,
                    border_color=styles["colors"]["divider"],
                    focused_border_color=styles["colors"]["primary"],
                    options=[
                        ft.dropdown.Option("0", locale["shop"]["item_types"]["0"]),  # Mini Box
                        ft.dropdown.Option("14", locale["shop"]["item_types"]["14"]),  # Big Box
                        ft.dropdown.Option("10", locale["shop"]["item_types"]["10"]),  # Mega Box
                        ft.dropdown.Option("1", locale["shop"]["item_types"]["1"]),  # Gold
                        ft.dropdown.Option("16", locale["shop"]["item_types"]["16"]),  # Diamonds
                        ft.dropdown.Option("7", locale["shop"]["item_types"]["7"]),  # Tickets
                        ft.dropdown.Option("9", locale["shop"]["item_types"]["9"]),  # Token Doubler
                        ft.dropdown.Option("17", locale["shop"]["item_types"]["17"]),  # Star Points
                        ft.dropdown.Option("3", locale["shop"]["item_types"]["3"]),  # Brawler
                        ft.dropdown.Option("4", locale["shop"]["item_types"]["4"]),  # Brawler Skin
                    ]
                )

                quantity_field = ft.TextField(
                    label=locale["shop"]["item_quantity_label"],
                    hint_text=locale["shop"]["item_quantity_hint"],
                    width=150,
                    border_radius=8,
                    border_color=styles["colors"]["divider"],
                    focused_border_color=styles["colors"]["primary"],
                    keyboard_type=ft.KeyboardType.NUMBER,
                    visible=True
                )

                fighter_name_field = ft.TextField(
                    label=locale["shop"]["fighter_name_label"],
                    hint_text=locale["shop"]["fighter_name_hint"],
                    width=200,
                    border_radius=8,
                    border_color=styles["colors"]["divider"],
                    focused_border_color=styles["colors"]["primary"],
                    visible=False
                )

                skin_name_field = ft.TextField(
                    label=locale["shop"]["skin_name_label"],
                    hint_text=locale["shop"]["skin_name_hint"],
                    width=200,
                    border_radius=8,
                    border_color=styles["colors"]["divider"],
                    focused_border_color=styles["colors"]["primary"],
                    visible=False
                )

                remove_item_button = ft.IconButton(
                    icon="delete",
                    icon_color=ERROR_COLOR if len(items_list) > 0 else BUTTON_DISABLED_COLOR,
                    disabled=len(items_list) <= 1,
                    on_click=lambda _: remove_item(item_id)
                )

                def on_item_type_change(e):
                    selected_type = item_type_dropdown.value
                    if selected_type == "3":  # Brawler
                        fighter_name_field.visible = True
                        skin_name_field.visible = False
                        quantity_field.visible = False
                    elif selected_type == "4":  # Skin
                        fighter_name_field.visible = False
                        skin_name_field.visible = True
                        quantity_field.visible = False
                    else:
                        fighter_name_field.visible = False
                        skin_name_field.visible = False
                        quantity_field.visible = True
                    update_create_button_state()
                    page.update()

                item_type_dropdown.on_change = on_item_type_change

                def on_quantity_change(e):
                    update_create_button_state()

                def on_fighter_name_change(e):
                    update_create_button_state()

                def on_skin_name_change(e):
                    update_create_button_state()

                quantity_field.on_change = on_quantity_change
                fighter_name_field.on_change = on_fighter_name_change
                skin_name_field.on_change = on_skin_name_change

                item_row = ft.Row([
                    item_type_dropdown,
                    quantity_field,
                    fighter_name_field,
                    skin_name_field,
                    remove_item_button
                ], spacing=10, alignment=ft.MainAxisAlignment.START)

                item_data = {
                    "id": item_id,
                    "row": item_row,
                    "type_dropdown": item_type_dropdown,
                    "quantity_field": quantity_field,
                    "fighter_name_field": fighter_name_field,
                    "skin_name_field": skin_name_field,
                    "remove_button": remove_item_button
                }

                items_list.append(item_data)
                items_container.controls.append(item_row)

                update_add_item_button_state()
                update_remove_item_buttons_state()
                update_create_button_state()
                page.update()

            def remove_item(item_id):
                if len(items_list) <= 1:
                    return

                item_to_remove = None
                for i, item in enumerate(items_list):
                    if item["id"] == item_id:
                        item_to_remove = item
                        for j in range(i + 1, len(items_list)):
                            items_list[j]["id"] = j - 1
                            items_list[j]["remove_button"].on_click = lambda _, id=j - 1: remove_item(id)
                        break

                if item_to_remove:
                    items_list.remove(item_to_remove)
                    items_container.controls.remove(item_to_remove["row"])

                    update_add_item_button_state()
                    update_remove_item_buttons_state()
                    update_create_button_state()
                    page.update()

            create_item_field()

            cancel_button = ft.ElevatedButton(
                content=ft.Row([
                    ft.Icon("cancel", size=18, color="white"),
                    ft.Text(locale["shop"]["cancel_button"], size=16, color="white")
                ], spacing=8, alignment=ft.CrossAxisAlignment.CENTER),
                bgcolor=BUTTON_DISABLED_COLOR,
                style=button_style,
                on_click=lambda _: show_shop_main()
            )

            error_text = ft.Text(size=12, color=styles["colors"]["error"], visible=False)

            def handle_create_promotion(e):

                if not is_promotion_valid():
                    page.update()
                    return

                name = promotion_name_field.value
                is_daily = is_daily_checkbox.value
                try:
                    start_time = int(start_time_field.value)
                except ValueError:
                    show_promotion_error(locale["shop"]["errors"]["enter_start_time"])
                    page.update()
                    return
                try:
                    end_time = int(end_time_field.value)
                except ValueError:
                    show_promotion_error(locale["shop"]["errors"]["enter_end_time"])
                    page.update()
                    return
                try:
                    price = float(price_field.value)
                except ValueError:
                    show_promotion_error(locale["shop"]["errors"]["enter_price"])
                    page.update()
                    return
                try:
                    old_price = float(old_price_field.value)
                except ValueError:
                    show_promotion_error(locale["shop"]["errors"]["enter_old_price"])
                    page.update()
                    return

                price_type = int(price_type_dropdown.value) if price_type_dropdown.value else None
                if price_type is None:
                    show_promotion_error(locale["shop"]["errors"]["select_price_type"])
                    page.update()
                    return

                background = background_field.value
                show_to_new_users = show_to_new_users_checkbox.value

                items_info = []
                validation_failed = False

                for i, item in enumerate(items_list):
                    item_type = int(item["type_dropdown"].value) if item["type_dropdown"].value else -1
                    if item_type == -1:
                        show_promotion_error(f"{locale['shop']['errors']['enter_item_type']} ({i + 1})")
                        validation_failed = True
                        break

                    item_data = {"type": item_type}

                    if item_type not in [3, 4]:
                        quantity_str = item["quantity_field"].value or ""
                        if not quantity_str:
                            show_promotion_error(f"{locale['shop']['errors']['enter_item_quantity']} ({i + 1})")
                            validation_failed = True
                            break
                        try:
                            item_data["quantity"] = int(quantity_str)
                            if item_data["quantity"] < 1 or item_data["quantity"] > 99999:
                                raise ValueError("Out of range")
                        except (ValueError, TypeError):
                            show_promotion_error(f"{locale['shop']['errors']['enter_item_quantity']} ({i + 1})")
                            validation_failed = True
                            break

                    if item_type == 3:  # Brawler
                        brawler_name = item["fighter_name_field"].value or ""
                        if not brawler_name.strip():
                            show_promotion_error(f"{locale['shop']['errors']['enter_fighter_name']} ({i + 1})")
                            validation_failed = True
                            break
                        item_data["brawler_name"] = brawler_name.strip()
                    elif item_type == 4:  # Skin
                        skin_name = item["skin_name_field"].value or ""
                        if not skin_name.strip():
                            show_promotion_error(f"{locale['shop']['errors']['enter_skin_name']} ({i + 1})")
                            validation_failed = True
                            break
                        item_data["skin_name"] = skin_name.strip()

                    items_info.append(item_data)

                if validation_failed:
                    page.update()
                    return

                if not shop_client.connected:
                    show_promotion_error(locale["shop"]["errors"]["grpc_connection_failed"])
                    page.update()
                    return

                success, message, promotion_id = shop_client.create_promotion(
                    name=name,
                    is_daily=is_daily,
                    start_time=start_time,
                    end_time=end_time,
                    price=price,
                    old_price=old_price,
                    price_type=price_type,
                    background=background,
                    show_to_new_users=show_to_new_users,
                    items_data=items_info
                )

                if success:
                    promotion_name_field.value = ""
                    start_time_field.value = ""
                    end_time_field.value = ""
                    price_field.value = ""
                    old_price_field.value = ""
                    price_type_dropdown.value = None
                    background_field.value = ""
                    is_daily_checkbox.value = False
                    show_to_new_users_checkbox.value = False

                    items_container.controls.clear()
                    items_list.clear()
                    create_item_field()

                    update_add_item_button_state()
                    update_remove_item_buttons_state()
                    
                    update_create_button_state()

                    show_promotion_success(message)
                else:
                    show_promotion_error(message)

                page.update()

            def show_promotion_error(message):
                dlg = ft.AlertDialog(
                    title=ft.Text("🤬"),
                    content=ft.Text(message),
                    actions=[
                        ft.TextButton("OK", on_click=lambda e: page.close(dlg))
                    ],
                )
                page.open(dlg)

            def show_promotion_success(message):
                dlg = ft.AlertDialog(
                    title=ft.Text("😂"),
                    content=ft.Text(message),
                    actions=[
                        ft.TextButton("OK", on_click=lambda e: page.close(dlg))
                    ],
                )
                page.open(dlg)

            def on_field_change(e):
                update_create_button_state()

            promotion_name_field.on_change = on_field_change
            start_time_field.on_change = on_field_change
            end_time_field.on_change = on_field_change
            price_field.on_change = on_field_change
            old_price_field.on_change = on_field_change
            price_type_dropdown.on_change = lambda e: update_create_button_state()

            current_shop_view.controls = [
                ft.Row([
                    ft.Text(locale["shop"]["create_promotion_title"], size=32, weight=ft.FontWeight.BOLD,
                            color=TEXT_PRIMARY),
                ], alignment=ft.MainAxisAlignment.START, spacing=15),
                ft.Text(locale["shop"]["subtitle"], size=16, color=TEXT_SECONDARY),
                ft.Divider(height=40, color="transparent"),
                ft.Card(
                    content=ft.Container(
                        content=ft.Column([
                            ft.Row([
                                ft.Icon("add_shopping_cart", color=PRIMARY_COLOR, size=24),
                                ft.Text(locale["shop"]["create_promotion_title"], size=22, weight=ft.FontWeight.W_600,
                                        color=TEXT_PRIMARY),
                            ], alignment=ft.MainAxisAlignment.START, spacing=12),
                            ft.Divider(height=25, color="transparent"),
                            promotion_name_field,
                            is_daily_checkbox,
                            start_time_field,
                            end_time_field,
                            price_field,
                            old_price_field,
                            price_type_dropdown,
                            background_field,
                            show_to_new_users_checkbox,
                            ft.Divider(height=25, color="transparent"),
                            ft.Text(locale["shop"]["items_title"], size=18, weight=ft.FontWeight.W_500,
                                    color=TEXT_PRIMARY),
                            add_item_button,
                            items_container,
                            ft.Divider(height=25, color="transparent"),
                            ft.Row([
                                create_button,
                                cancel_button
                            ], spacing=15)
                        ], spacing=10),
                        padding=CARD_PADDING,
                        border_radius=CARD_BORDER_RADIUS
                    ),
                    elevation=8,
                    color=SURFACE_COLOR
                )
            ]
            page.update()

        show_shop_main()

        current_view.content = ft.Column([
            current_shop_view
        ], scroll=ft.ScrollMode.AUTO)
        
        page.update()

    def show_stats():
        hashtag_factory = HashtagFieldFactory(page, locale, styles)

        player_id_field = hashtag_factory.create_hashtag_field(
            label=locale["moderation"]["user_id_label"],
            hint_text=locale["moderation"]["user_id_hint"]
        )

        player_stats_container = ft.Column([], visible=False)

        def update_view_button_state(valid_data):
            if valid_data is not None:
                view_button.disabled = False
                view_button.bgcolor = PRIMARY_COLOR
            else:
                view_button.disabled = True
                view_button.bgcolor = BUTTON_DISABLED_COLOR
            page.update()

        player_id_field = hashtag_factory.create_hashtag_field(
            label=locale["moderation"]["user_id_label"],
            hint_text=locale["moderation"]["user_id_hint"],
            on_valid_change=update_view_button_state
        )

        view_button = ft.ElevatedButton(
            content=ft.Row([
                ft.Icon("visibility", size=18, color="white"),
                ft.Text(locale["statistics"]["player_search"]["view_button"], size=16, color="white")
            ], spacing=8, alignment=ft.CrossAxisAlignment.CENTER),
            bgcolor=BUTTON_DISABLED_COLOR,
            style=button_style,
            disabled=True,
            on_click=lambda _: handle_view_player_stats(player_id_field.get_current_data())
        )

        def handle_view_player_stats(data):
            if data is None:
                error_control = player_id_field.error_text
                error_control.value = locale["moderation"]["errors"]["enter_valid_id"]
                error_control.visible = True
                page.update()
                return

            player_stats = stats_client.get_player_stats(data)

            player_stats_container.controls.clear()

            ban_info = ft.Column([
                ft.Text(locale["statistics"]["player_stats"]["ban_info"], size=18, weight=ft.FontWeight.BOLD,
                        color=TEXT_PRIMARY),
                ft.Text(
                    f"{locale['statistics']['player_stats']['is_banned']}: {locale['statistics']['player_stats']['yes'] if player_stats['is_banned'] else locale['statistics']['player_stats']['no']}",
                    size=14, color=TEXT_PRIMARY),
            ], spacing=5)

            if player_stats['is_banned']:
                ban_info.controls.append(
                    ft.Text(f"{locale['statistics']['player_stats']['ban_reason']}: {player_stats['ban_reason']}",
                            size=14, color=TEXT_SECONDARY))
                ban_info.controls.append(
                    ft.Text(f"{locale['statistics']['player_stats']['ban_end_time']}: {player_stats['ban_end_time']}",
                            size=14, color=TEXT_SECONDARY))

            player_stats_container.controls.append(ban_info)
            player_stats_container.controls.append(ft.Divider(height=20, color=DIVIDER_COLOR))

            lock_info = ft.Column([
                ft.Text(locale["statistics"]["player_stats"]["lock_info"], size=18, weight=ft.FontWeight.BOLD,
                        color=TEXT_PRIMARY),
                ft.Text(
                    f"{locale['statistics']['player_stats']['is_locked']}: {locale['statistics']['player_stats']['yes'] if player_stats['is_locked'] else locale['statistics']['player_stats']['no']}",
                    size=14, color=TEXT_PRIMARY),
            ], spacing=5)

            if player_stats['is_locked']:
                lock_info.controls.append(
                    ft.Text(f"{locale['statistics']['player_stats']['unlock_code']}: {player_stats['unlock_code']}",
                            size=14, color=TEXT_SECONDARY))

            player_stats_container.controls.append(lock_info)
            player_stats_container.controls.append(ft.Divider(height=20, color=DIVIDER_COLOR))

            general_info = ft.Column([
                ft.Text(locale["statistics"]["player_stats"]["general_info"], size=18, weight=ft.FontWeight.BOLD,
                        color=TEXT_PRIMARY),
                ft.Text(f"{locale['statistics']['player_stats']['sessions_count']}: {player_stats['sessions_count']}",
                        size=14, color=TEXT_PRIMARY),
                ft.Text(f"{locale['statistics']['player_stats']['creation_time']}: {player_stats['creation_time']}",
                        size=14, color=TEXT_PRIMARY),
                ft.Text(f"{locale['statistics']['player_stats']['play_time']}: {player_stats['play_time_seconds']}",
                        size=14, color=TEXT_PRIMARY),
                ft.Text(f"{locale['statistics']['player_stats']['device_language']}: {player_stats['device_language']}",
                        size=14, color=TEXT_PRIMARY),
            ], spacing=5)

            player_stats_container.controls.append(general_info)
            player_stats_container.controls.append(ft.Divider(height=20, color=DIVIDER_COLOR))

            devices_info = ft.Column([
                ft.Text(locale["statistics"]["player_stats"]["devices_info"], size=18, weight=ft.FontWeight.BOLD,
                        color=TEXT_PRIMARY),
            ], spacing=5)

            for device, count in player_stats['devices'].items():
                devices_info.controls.append(
                    ft.Text(f"{device}: {count} {locale['statistics']['entries']}", size=14, color=TEXT_SECONDARY))

            player_stats_container.controls.append(devices_info)
            player_stats_container.controls.append(ft.Divider(height=20, color=DIVIDER_COLOR))

            servers_info = ft.Column([
                ft.Text(locale["statistics"]["player_stats"]["servers_info"], size=18, weight=ft.FontWeight.BOLD,
                        color=TEXT_PRIMARY),
            ], spacing=5)

            for server, count in player_stats['servers_connected'].items():
                servers_info.controls.append(
                    ft.Text(f"{server}: {count} {locale['statistics']['connections']}", size=14, color=TEXT_SECONDARY))

            player_stats_container.controls.append(servers_info)
            player_stats_container.controls.append(ft.Divider(height=20, color=DIVIDER_COLOR))

            ips_info = ft.Column([
                ft.Text(locale["statistics"]["player_stats"]["clients_info"], size=18, weight=ft.FontWeight.BOLD,
                        color=TEXT_PRIMARY),
            ], spacing=5)

            for ip, count in player_stats['client_ips'].items():
                ips_info.controls.append(
                    ft.Text(f"{ip}: {count} {locale['statistics']['connections']}", size=14, color=TEXT_SECONDARY))

            player_stats_container.controls.append(ips_info)

            player_stats_container.visible = True
            page.update()

        global_stats = stats_client.get_global_stats()

        current_view.content = ft.Column([
            ft.Row([
                ft.Text(locale["statistics"]["title"], size=32, weight=ft.FontWeight.BOLD, color=TEXT_PRIMARY),
                ft.Container(
                    content=ft.Text(locale["statistics"]["live_status"], size=12, color=SUCCESS_COLOR,
                                    weight=ft.FontWeight.BOLD),
                    bgcolor="#1b5e20",
                    padding=ft.Padding(8, 4, 8, 4),
                    border_radius=12
                )
            ], alignment=ft.MainAxisAlignment.START, spacing=15),
            ft.Text(locale["statistics"]["subtitle"], size=16, color=TEXT_SECONDARY),
            ft.Divider(height=40, color="transparent"),

            ft.Card(
                content=ft.Container(
                    content=ft.Column([
                        ft.Row([
                            ft.Icon("bar_chart", color=SECONDARY_COLOR, size=24),
                            ft.Text(locale["statistics"]["global_stats"]["title"], size=22, weight=ft.FontWeight.W_600,
                                    color=TEXT_PRIMARY),
                        ], alignment=ft.MainAxisAlignment.START, spacing=12),
                        ft.Divider(height=25, color="transparent"),
                        ft.Row([
                            ft.Container(
                                content=ft.Column([
                                    ft.Text(str(global_stats["active_players"]), size=24, weight=ft.FontWeight.BOLD,
                                            color=PRIMARY_COLOR),
                                    ft.Text(locale["statistics"]["global_stats"]["active_players"], size=14,
                                            color=TEXT_SECONDARY)
                                ], horizontal_alignment=ft.CrossAxisAlignment.CENTER),
                                padding=20,
                                border_radius=12,
                                bgcolor=SURFACE_COLOR,
                                expand=True
                            ),
                            ft.Container(
                                content=ft.Column([
                                    ft.Text(str(global_stats["active_battles"]), size=24, weight=ft.FontWeight.BOLD,
                                            color=PRIMARY_COLOR),
                                    ft.Text(locale["statistics"]["global_stats"]["active_battles"], size=14,
                                            color=TEXT_SECONDARY)
                                ], horizontal_alignment=ft.CrossAxisAlignment.CENTER),
                                padding=20,
                                border_radius=12,
                                bgcolor=SURFACE_COLOR,
                                expand=True
                            ),
                            ft.Container(
                                content=ft.Column([
                                    ft.Text(str(global_stats["players"]), size=24, weight=ft.FontWeight.BOLD,
                                            color=PRIMARY_COLOR),
                                    ft.Text(locale["statistics"]["global_stats"]["players"], size=14,
                                            color=TEXT_SECONDARY)
                                ], horizontal_alignment=ft.CrossAxisAlignment.CENTER),
                                padding=20,
                                border_radius=12,
                                bgcolor=SURFACE_COLOR,
                                expand=True
                            ),
                            ft.Container(
                                content=ft.Column([
                                    ft.Text(str(global_stats["alliances"]), size=24, weight=ft.FontWeight.BOLD,
                                            color=PRIMARY_COLOR),
                                    ft.Text(locale["statistics"]["global_stats"]["alliances"], size=14,
                                            color=TEXT_SECONDARY)
                                ], horizontal_alignment=ft.CrossAxisAlignment.CENTER),
                                padding=20,
                                border_radius=12,
                                bgcolor=SURFACE_COLOR,
                                expand=True
                            ),
                            ft.Container(
                                content=ft.Column([
                                    ft.Text(str(global_stats["game_rooms"]), size=24, weight=ft.FontWeight.BOLD,
                                            color=PRIMARY_COLOR),
                                    ft.Text(locale["statistics"]["global_stats"]["game_rooms"], size=14,
                                            color=TEXT_SECONDARY)
                                ], horizontal_alignment=ft.CrossAxisAlignment.CENTER),
                                padding=20,
                                border_radius=12,
                                bgcolor=SURFACE_COLOR,
                                expand=True
                            ),
                        ], spacing=20),
                    ], spacing=15),
                    padding=25,
                    border_radius=CARD_BORDER_RADIUS
                ),
                elevation=8,
                color=SURFACE_COLOR
            ),

            ft.Divider(height=35, color="transparent"),

            ft.Card(
                content=ft.Container(
                    content=ft.Column([
                        ft.Row([
                            ft.Icon("person_search", color=PRIMARY_COLOR, size=24),
                            ft.Text(locale["statistics"]["player_search"]["title"], size=22, weight=ft.FontWeight.W_600,
                                    color=TEXT_PRIMARY),
                        ], alignment=ft.MainAxisAlignment.START, spacing=12),
                        ft.Divider(height=25, color="transparent"),
                        player_id_field,
                        ft.Divider(height=15, color="transparent"),
                        view_button,
                        ft.Divider(height=25, color="transparent"),
                        player_stats_container
                    ], spacing=10),
                    padding=CARD_PADDING,
                    border_radius=CARD_BORDER_RADIUS
                ),
                elevation=8,
                color=SURFACE_COLOR
            ),
        ], spacing=10, scroll=ft.ScrollMode.AUTO)
        page.update()

    def create_menu_button(text, icon, on_click):
        def wrapper(e):
            nonlocal active_menu_button

            if active_menu_button:
                active_menu_button.style = menu_button_style
                active_menu_button.bgcolor = MENU_ITEM_BG

            e.control.style = ft.ButtonStyle(
                shape=ft.RoundedRectangleBorder(radius=BUTTON_BORDER_RADIUS),
                padding=ft.Padding(*MENU_BUTTON_PADDING),
                bgcolor=MENU_ITEM_ACTIVE,
                color=PRIMARY_COLOR,
                overlay_color=MENU_ITEM_HOVER
            )
            active_menu_button = e.control
            on_click()
            page.update()

        return ft.ElevatedButton(
            content=ft.Row([
                ft.Icon(icon, size=20, color=PRIMARY_COLOR if active_menu_button else TEXT_SECONDARY),
                ft.Text(text, size=16, color=TEXT_PRIMARY)
            ], spacing=12, alignment=ft.CrossAxisAlignment.CENTER),
            style=menu_button_style,
            width=190,
            on_click=wrapper
        )

    def create_locale_selector():
        return ft.Dropdown(
            options=[
                ft.dropdown.Option("ru", "Русский"),
                ft.dropdown.Option("en", "English"),
                ft.dropdown.Option("zh", "中文"),
            ],
            value=current_locale,
            width=150,
            on_change=lambda e: change_locale(e.control.value)
        )

    def create_menu():
        nonlocal menu_container

        menu_buttons = [
            create_menu_button(locale["menu"]["moderation"], "security", show_moderation),
            create_menu_button(locale["menu"]["messages"], "forum", show_messages),
            create_menu_button(locale["menu"]["gifts"], "card_giftcard", show_gifts),
            create_menu_button(locale["menu"]["shop"], "shopping_cart", show_shop),
            create_menu_button(locale["menu"]["statistics"], "analytics", show_stats),
        ]

        menu_content = ft.Column(
            [
                ft.Container(
                    content=ft.Column([
                        ft.Text(locale["menu"]["title_main"], size=20, weight=ft.FontWeight.BOLD, color=PRIMARY_COLOR),
                        ft.Text(locale["menu"]["title_sub"], size=18, weight=ft.FontWeight.W_300, color=TEXT_SECONDARY)
                    ],
                        spacing=2,
                        horizontal_alignment=ft.CrossAxisAlignment.CENTER),
                    padding=ft.Padding(0, 25, 0, 35),
                    alignment=ft.alignment.center
                ),

                ft.Divider(height=1, color=DIVIDER_COLOR),
                *menu_buttons,
                ft.Divider(height=1, color=DIVIDER_COLOR),

                ft.Container(
                    content=create_locale_selector(),
                    padding=ft.Padding(20, 10, 20, 10)
                ),

                ft.Container(
                    content=ft.Column([
                        ft.Text(locale["menu"]["system_status"], size=12, color=TEXT_SECONDARY,
                                weight=ft.FontWeight.W_500),
                        ft.Row([
                            ft.Container(
                                width=12,
                                height=12,
                                border_radius=6,
                                bgcolor=SUCCESS_COLOR
                            ),
                            ft.Text(locale["menu"]["system_online"], size=14, color=TEXT_PRIMARY)
                        ], spacing=8, alignment=ft.MainAxisAlignment.START),
                        ft.Text(locale["menu"]["system_version"], size=12, color=TEXT_SECONDARY)
                    ], spacing=8),
                    padding=ft.Padding(20, 20, 20, 10)
                )
            ],
            spacing=8,
            alignment=ft.MainAxisAlignment.START,
            horizontal_alignment=ft.CrossAxisAlignment.CENTER
        )

        menu_container.content = menu_content
        page.update()

    def wrap_show_func(func):
        def wrapper():
            page.current_view_func = wrapper
            func()

        return wrapper

    show_moderation = wrap_show_func(show_moderation)
    show_messages = wrap_show_func(show_messages)
    show_gifts = wrap_show_func(show_gifts)
    show_shop = wrap_show_func(show_shop)
    show_stats = wrap_show_func(show_stats)

    menu_container = ft.Container(
        width=MENU_WIDTH,
        bgcolor=MENU_BG,
        padding=ft.Padding(10, 0, 10, 0),
        border=ft.border.BorderSide(1, DIVIDER_COLOR)
    )

    create_menu()

    layout = ft.Row(
        [
            menu_container,
            ft.VerticalDivider(width=1, color=DIVIDER_COLOR),
            current_view
        ],
        expand=True
    )
    
    page.add(layout)
    show_moderation()


if __name__ == "__main__":
    import os
    
    base_dir = os.path.dirname(os.path.abspath(__file__))
    assets_dir_path = os.path.join(base_dir, "assets")
    
    ft.app(target=main, assets_dir=assets_dir_path)