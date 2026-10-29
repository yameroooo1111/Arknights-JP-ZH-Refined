"""Release selection must never switch this fork back to upstream or to a source ZIP."""
import tempfile
import unittest
import json
from pathlib import Path
from arklocalizer.launcher_backend import LauncherConfig, APP_NAME
from arklocalizer.libraries import REPOSITORY, official_id
from arklocalizer.updates import check_updates, EXE_NAME


class FakeGitHub:
    def __init__(self): self.urls=[]
    def json(self,url,optional=False):
        self.urls.append(url)
        assert url == 'https://api.github.com/repos/yameroooo1111/Arknights-JP-ZH-Refined/releases/latest'
        return {'tag_name':'v2.2.0','html_url':'https://github.com/yameroooo1111/Arknights-JP-ZH-Refined/releases/tag/v2.2.0',
                'assets':[{'name':'Arknights-JP-ZH-Refined-v2.2.0-source.zip'},
                          {'name':'Arknights-JP-ZH-Refined-v2.2.0-win-x64.zip'}]}


class ReleaseTests(unittest.TestCase):
    def test_release_only_repository(self):
        class ReleaseOnly(FakeGitHub):
            def __init__(self): super().__init__(); self.messages=[]
            def log(self,text):self.messages.append(text)
            def json(self,url,optional=False):
                if url.endswith('/releases/latest'):return super().json(url,optional)
                if url.endswith('/commits/main'):return {'sha':'a'*40}
                if url.endswith('/Arknights-JP-ZH-Refined'):return {'default_branch':'main'}
                if url.endswith('ARKLOCALIZER_MANIFEST.json') and optional:return None
                raise AssertionError(url)
        client=ReleaseOnly()
        with tempfile.TemporaryDirectory() as root:
            library=Path(root)/'runtime/jp-zh-offline-final';library.mkdir(parents=True)
            (library/'ARKLOCALIZER_MANIFEST.json').write_text(json.dumps({'source_locale':'jp','files':[]}),encoding='utf8')
            (library/'WORDLIBRARY.json').write_text(json.dumps({'id':official_id('jp'),'locale':'jp','name':'Refined','repository':REPOSITORY,'remote_path':'runtime/jp-zh-offline-final'}),encoding='utf8')
            result=check_updates(Path(root),LauncherConfig(),client=client)
        self.assertFalse(result['errors'])
        self.assertEqual(result['libraries'],[])
        self.assertTrue(result['software']['available'])
        self.assertTrue(client.messages)

    def test_own_release_and_binary_asset(self):
        client=FakeGitHub()
        with tempfile.TemporaryDirectory() as root:
            result=check_updates(Path(root),LauncherConfig(check_library_updates=False),client=client)
        self.assertFalse(result['errors'])
        self.assertTrue(result['software']['available'])
        self.assertTrue(result['software']['asset']['name'].endswith('-win-x64.zip'))
        self.assertEqual(len(client.urls),1)

    def test_independent_configuration(self):
        self.assertEqual(APP_NAME,'ArknightsJPZHRefined')
        self.assertEqual(REPOSITORY,'yameroooo1111/Arknights-JP-ZH-Refined')
        self.assertEqual(EXE_NAME,'ArknightsRefined.exe')
        self.assertEqual(LauncherConfig().locale,'jp')
        self.assertNotEqual(official_id('jp'),official_id('en'))

if __name__=='__main__':unittest.main()
