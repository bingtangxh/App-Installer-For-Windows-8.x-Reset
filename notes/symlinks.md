# 关于该项目中的一些符号链接

该项目由于原作者开发方便，使用了一些符号链接。在迁移时会遇到问题。

符号链接主要是位于 \appinstaller 目录中。

使用了符号链接的文件有：

- certmgr.h
- notice.h
- pkgmgr.h
- pkgread.h
- priformatcli.h

以上文件分别指向的是 `..\<ProjectName>\<ProjectName>.h` 。

# 一些最终运行所需文件位于 shared 目录

此外，在生成完成后，最终生成的程序需要一些文件，但是这些文件位于的是解决方案的 `shared` 目录。

因此，你在测试时，需要将 `shared` 目录中的每一个子目录都在输出目录下（`Debug`和`Release`）创建一个目录符号链接 (`SYMLINKD`)，指向 `..\shared\<子目录>` 。

例如：

``` for /d %A in (D:\GitHub\App-Installer-For-Windows-8.x-Reset\shared\*) do @mklink /d "D:\GitHub\App-Installer-For-Windows-8.x-Reset\Debug\%~nA" "%A" ```

``` for /d %A in (D:\GitHub\App-Installer-For-Windows-8.x-Reset\shared\*) do @mklink /d "D:\GitHub\App-Installer-For-Windows-8.x-Reset\Release\%~nA" "%A" ```

最终发布时，你也需要将 `shared` 目录中的每一个子目录都复制到发布目录，再打包发布。

# pkgread 项目需要引用 pugixml 代码文件

最后，pkgread 项目引用了我新建的一个叫 pugixml 的项目，包含如下文件：

- `pugixml.cpp`
- `pugixml.hpp`
- `pugiconfig.hpp`

如果没有这个的话，链接器会报错 LNK2019 无法解析的外部符号：

- `pugi::xml_document::load_file`
- `pugi::as_utf8`
- `pugi::as_wide`

和 LNK1120 n 个无法解析的外部命令。

因为这个解决方案需要一个 NuGet 包叫 `pugixml` ，
但是这个包默认只有 x86 和 x64 的版本，因此为了编译出 ARM32 版本，我就把 pugixml.cpp 等文件单开了一个项目。

迁移之后，你应该需要在 pugixml 项目中重新添加这几个“现有项”的引用。

路径示例：

```D:\GitHub\App-Installer-For-Windows-8.x-Reset\packages\pugixml.1.15.0\build\native\include\pugixml.cpp```

我的做法是在整个解决方案中添加了一个新的项目 `pugixml`，这个项目的唯一作用就是编译 pugixml.cpp 生成 pugixml.lib 。

暂时还需要手动将 Debug 和 Release 版本的 pugixml.lib 从生成的路径分别复制到项目生成时所引用的 Debug 和 Release 目录下，才能成功链接。

例如：

生成的目标路径：

```D:\GitHub\App-Installer-For-Windows-8.x-Reset\ARM\Debug\pugixml.lib```

```D:\GitHub\App-Installer-For-Windows-8.x-Reset\ARM\Release\pugixml.lib```


你需要复制到的路径：

```D:\GitHub\App-Installer-For-Windows-8.x-Reset\packages\pugixml.1.15.0\build\native\lib\ARM\v140\dynamic\Debug\pugixml.lib```

```D:\GitHub\App-Installer-For-Windows-8.x-Reset\packages\pugixml.1.15.0\build\native\lib\ARM\v140\dynamic\Release\pugixml.lib```

