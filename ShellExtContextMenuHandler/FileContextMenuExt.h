#pragma once

#include <windows.h>
#include <shlobj.h>     // For IShellExtInit and IContextMenu
#include <string>
#include <vector>

// Command ID offsets used in QueryContextMenu/InvokeCommand
enum
{
	IDM_CONVERT_JPG = 0,
	IDM_CONVERT_PNG = 1,
};

class FileContextMenuExt : public IShellExtInit, public IContextMenu
{
public:
    // IUnknown
    IFACEMETHODIMP QueryInterface(REFIID riid, void **ppv) override;
    IFACEMETHODIMP_(ULONG) AddRef() override;
    IFACEMETHODIMP_(ULONG) Release() override;

    // IShellExtInit
    IFACEMETHODIMP Initialize(LPCITEMIDLIST pidlFolder, LPDATAOBJECT pDataObj, HKEY hKeyProgID) override;

    // IContextMenu
    IFACEMETHODIMP QueryContextMenu(HMENU hMenu, UINT indexMenu, UINT idCmdFirst, UINT idCmdLast, UINT uFlags) override;
    IFACEMETHODIMP InvokeCommand(LPCMINVOKECOMMANDINFO pCommandInfo) override;
    IFACEMETHODIMP GetCommandString(UINT_PTR idCommand, UINT uFlags, UINT *pwReserved, LPSTR pszName, UINT cchMax) override;
	
    FileContextMenuExt(void);

protected:
    ~FileContextMenuExt(void);

private:
    // Reference count of component.
    long m_cRef;

    // The name of the selected files.
	std::vector<std::wstring> m_vSelectedFiles;

    // Resource instance to use for localized strings (defaults to module instance)
    HMODULE m_hResourceInstance;

    // Buffers for localized menu strings (must persist beyond QueryContextMenu)
    wchar_t m_menuTextBuf[256];
    wchar_t m_toJpgTextBuf[128];
    wchar_t m_toPngTextBuf[128];

    // Handlers for conversion submenu
    void OnConvertToJpg(HWND hWnd);
    void OnConvertToPng(HWND hWnd);

	bool FileContextMenuExt::RunConverterCommand(HWND hWnd, PCWSTR targetFormat);

    PWSTR m_pszMenuText;
    PCSTR m_pszVerb;
    PCWSTR m_pwszVerb;
    PCWSTR m_pwszVerbCanonicalName;
    PCWSTR m_pwszVerbHelpText;
};
