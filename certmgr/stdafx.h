// stdafx.h : 标准系统包含文件的包含文件，
// 或是经常使用但不常更改的
// 特定于项目的包含文件
//

#pragma once
#ifdef _M_ARM
#define _ARM_WINAPI_PARTITION_DESKTOP_SDK_AVAILABLE 1
#endif

#include "targetver.h"

#define WIN32_LEAN_AND_MEAN             // 从 Windows 头中排除极少使用的资料
// Windows 头文件: 
#include <windows.h>

// TODO:  在此处引用程序需要的其他头文件
#include <wincrypt.h>
#include <softpub.h>
#include <string>
#include <vector>
#include <shellapi.h>
#include <functional>