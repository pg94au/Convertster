#pragma once

#include <windows.h>
#include <shlobj.h>     // For IShellExtInit and IContextMenu
#include <string>
#include <vector>

// Command ID offsets used in QueryContextMenu/InvokeCommand
enum
{
	IDM_DISPLAY = 0,
	IDM_CONVERT_JPG = 1,
	IDM_CONVERT_PNG = 2,
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
    IFACEMETHODIMP InvokeCommand(LPCMINVOKECOMMANDINFO pici) override;
    IFACEMETHODIMP GetCommandString(UINT_PTR idCommand, UINT uFlags, UINT *pwReserved, LPSTR pszName, UINT cchMax) override;
	
    FileContextMenuExt(void);

protected:
    ~FileContextMenuExt(void);

private:
    // Reference count of component.
    long m_cRef;

    // The name of the selected files.
	std::vector<std::wstring> m_vSelectedFiles;

    // The method that handles the "display" verb.
    void OnVerbDisplayFileName(HWND hWnd);

    // Handlers for conversion submenu
    void OnConvertToJpg(HWND hWnd);
    void OnConvertToPng(HWND hWnd);

    PWSTR m_pszMenuText;
    PCSTR m_pszVerb;
    PCWSTR m_pwszVerb;
    PCWSTR m_pwszVerbCanonicalName;
    PCWSTR m_pwszVerbHelpText;
};
