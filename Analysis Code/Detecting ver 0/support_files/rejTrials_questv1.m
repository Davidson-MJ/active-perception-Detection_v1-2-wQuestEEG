% rejTrials_questv1%

% certain bad trials (identified in figure folder).
% look at 'TrialHeadYPeaks', to see if the tracking dropped out on any
% trials. Or if participants were not walking smoothly.



% step in to reject particular ppant+trial combos.
badtrials=[];
switch subjID
    %%%% Quest v1 (contrast jitter)
    case 'AG_2022-02-17-11-14'
        badtrials = 41;
    case 'AH_2022-03-08-02-01'
        badtrials = [41:42,51,161,184, 186,193];
    case 'AP_2022-03-14-03-15'
        badtrials =[41:42, 51,70:71,73];
    case 'AW_2022-03-01-10-09'
        badtrials = [41, 51,91,152,161];
    case 'BW_2022-02-25-11-21'
        badtrials= 61;
        
    case 'CP_2022-02-24-04-37'
        badtrials = [61,63];
        
    case 'CS_2022-02-17-10-39'
        badtrials = 41;
    case 'DV_2022-02-08-11-13'
        badtrials= [43, 161];
        
    case 'EL_2022-03-08-12-59'
        badtrials = 141;
    case 'IF_2022-02-11-10-16'
        badtrials = [81,82];
        
    case 'IL_2022-03-07-02-15'
        badtrials = [21,33];
    case 'KP_2022-02-24-02-52'
        badtrials = [41, 120,121];
    case 'KS_2022-02-15-03-11'
        badtrials = [41:43, 59];
    case 'LLq_2022-02-03-02-22'
        %no bad trials
    case 'MD01_2021-12-21-01-08' %%%% Quest v1 (contrast jitter)   
        badtrials=179;
    case 'MF_2022-02-09-02-52'
        badtrials= [21:23,61];
  
    case 'NS_2022-02-10-11-02'
        badtrials = [41:43, 116];
    case 'SA_2022-03-08-10-11'
        badtrials = 21;
    case 'SY_2022-03-08-11-58'
        badtrials =  [41,81,168,189];
    case 'VS_2022-02-10-11-02'
        badtrials= [94];
        
    case 'YH_2022-02-08-03-13'
        badtrials=[41, 165];
    case 'YW_2022-02-21-02-27'
        badtrials = 41;
        
        %%%%%%%%%%% 
        %%%%%%%%%%% rejected:
        %%%%%%%%%%% 
    
    case  'EC02_2021-12-21-12-07' % very high performance accuracy (failed staircase?)  
        badtrials= [144]; 
    case 'KM01_2021-13-14-11-12' %%%%  Quest v1 (contrast jitter) - poor performance.
        badtrials=[71];
        
    case 'JL_2022-02-10-10-11' % rejected (failed calibration (90% acc)
        badtrials = [41,42,48, 141, 162];
    case 'MX_2022-02-09-03-39'
        %no bad trials
    case 'TT_2022-02-08-02-11' %rejected (failed calibration)
          badtrials = [21:24, 67];
        
        %%%% PILOT DATA:
        
    case 'CT01_2021-12-02-03-54' % Pilot
        %no bad trials
    case 'EC01_2021-11-30-03-23' % Pilot
        badtrials=94;
    case 'EC01_2021-12-02-10-02' % Pilot
        badtrials=23;
   
    case 'LL01_2021-12-06-02-38' % pilot (no targs absent)
        %no bad trials.
    case 'LT01_2021-12-06-10-41' % pilot
        badtrials= [161,162];
   
    case 'YAXIN01_2021-11-30-01-07' % pilot
           %no bad trials
    case 'md02_2021-12-06-03-23' % pilot
        %no bad trials.
    
    case 'md03_2021-11-17-03-36' % pilot
        badtrials  =[107];
    case 'md04_2021-11-17-06-20' % pilot
        %no bad trials.
    case 'mjdNEW_2021-12-07-02-36' % pilot
        badtrials = 121;
    case 'rtk01_2021-11-17-05-12' % pilot
        badtrials= [53, 54];
   
end

    
%%
if ismember(itrial,badtrials)
    disp(['Skipping bad trial ' num2str(itrial) ' for ' subjID]);
    skip=1;
end
%%