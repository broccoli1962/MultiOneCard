namespace GoogleSpreadSheetLoader
{
    internal class GSSL_State
    {
        public enum eGSSL_State
        {
            None,
            Prepare,
            DownloadingSpreadSheet,
            DownloadingSheet,
            GenerateTableScript,
            GenerateTableData,
            GenerateTableLinker,
            Done,
        }

        public static eGSSL_State CurrState => _currState;
        private static eGSSL_State _currState;
        private static string _progressValue; // ex) (0/0)
        public static string ProgressText => _progressText;
        private static string _progressText; // ex) 스프레드 시트 다운 진행중 (0/0)

        internal static void SetProgressState(eGSSL_State state, string progressValue ="")
        {
            _currState = state;

            _progressValue = progressValue;

            string stepValue = $"({(int)_currState}/{(int)eGSSL_State.Done})";
            _progressText = _currState switch
            {
                eGSSL_State.None => "",
                eGSSL_State.Prepare => "준비 중",
                eGSSL_State.DownloadingSpreadSheet => $"{stepValue} 스프레드 시트 다운로드 중 {progressValue}",
                eGSSL_State.DownloadingSheet => $"{stepValue} 시트 다운로드 중 {progressValue}",
                eGSSL_State.GenerateTableScript => $"{stepValue} 테이블 스크립트 생성 중",
                eGSSL_State.GenerateTableData => $"{stepValue} 테이블 데이터 생성 중",
                eGSSL_State.GenerateTableLinker => $"{stepValue} 테이블 링커 생성 중",
                eGSSL_State.Done => "완료",
                _ => "정의되지 않은 상태",
            };
        }
    }
}