// priread.cpp : 定义 DLL 应用程序的导出函数。
//

#include "stdafx.h"
#include "priread.h"


// 这是导出变量的一个示例
PRIREAD_API int npriread=0;

// 这是导出函数的一个示例。
PRIREAD_API int fnpriread(void)
{
    return 42;
}

// 这是已导出类的构造函数。
// 有关类定义的信息，请参阅 priread.h
Cpriread::Cpriread()
{
    return;
}
