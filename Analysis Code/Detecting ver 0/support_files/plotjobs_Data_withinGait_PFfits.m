% plotjobs_Data_winGait_PFfits-

% loads the ppant data collated in j2_binData_bycycle./
% j3_binDatabylinkedcycles.

%plots the psychometric fits per condition (walk, stand), as well as fits 
% as a function of gait cycle (splitting into thirds?).

%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%

%%%%%% QUEST DETECT version %%%%%%
%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
%Mac:
% datadir='/Users/matthewdavidson/Documents/GitHub/active-perception-Detection_v1-1wQuest/Analysis Code/Detecting ver 0/Raw_data';
% PC:
datadir='C:\Users\User\Documents\matt\GitHub\active-perception-Detection_v1-1wQuest\Analysis Code\Detecting ver 0\Raw_data';
% laptop:
% datadir='C:\Users\vrlab\Documents\GitHub\active-perception-Detection_v1-1wQuest\Analysis Code\Detecting ver 0\Raw_data';


cd([datadir filesep 'ProcessedData']);
pfols= dir([pwd  filesep '*summary_data.mat']);
nsubs= length(pfols);
%%
%show ppant list:
tr= table([1:length(pfols)]',{pfols(:).name}' );
disp(tr)

%%
job.concat_GFX=1;

%participant level effects:
job.plot_PsychmFits_pfx=1; % compare types (walking, standing, L/R ft).
job.plot_PsychmFits_gait_pfx=1; % compare within Gait Cycle

%group level effects:
job.plot_PsychmFits_GFX=1;
job.plot_PsychmFits_gait_GFX=1; % compare within Gait Cycle

%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
%data wrangling first: Concat, and create null distribution.
pidx1=ceil(linspace(1,100,8)); % length 7
pidx2= ceil(linspace(1,200,14));% length 13
%%
nquants= 3; % how many quantiles to subdivide gait cycle?
gaittypes = {'single gait' , 'double gait'};
%%
if job.concat_GFX
   %preallocate storage:
    dataINrespPos=[];
    
    GFX_headY=[];   
    GFX_TargPosData=[];
    GFX_RespPosData=[];
    subjIDs={};
    
    for ippant =1:nsubs
        cd([datadir filesep 'ProcessedData'])    %%load data from import job.
        load(pfols(ippant).name, ...
            'Ppant_gaitData', 'Ppant_gaitData_doubGC','PFX_headY', 'PFX_headY_doubleGC', 'subjID', 'rawSummary_table');
        
        subjIDs{ippant} = subjID;
      
        
        
        % first retrieve index for separate gaits (L/Right feet).
        Ltrials= contains(Ppant_gaitData.pkFoot, 'LR');
        Rtrials= contains(Ppant_gaitData.pkFoot, 'RL');
        Ltrials_db= contains(Ppant_gaitData_doubGC.pkFoot, 'LRL');
        Rtrials_db= contains(Ppant_gaitData_doubGC.pkFoot, 'RLR');
    
        % mean head pos:        
        GFX_headY(ippant).gc = nanmean(PFX_headY);
        GFX_headY(ippant).doubgc = nanmean(PFX_headY_doubleGC);
        
        %% for standing still data, prepare for PF fits.
        T=rawSummary_table;
        
        %add extra column for RT data. (click - targonset).
        T.clickRT = T.targRT- T.targOnset ;
        
        targPrestrials =find(T.nTarg>0);
        practIndex = find(T.isPrac ==1);
        ss= size(practIndex,1);
        practIndex(ss+1) = practIndex(ss)+1;
        
        
        %note that per ppant, we may also need to reject trials from 
%         rejTrials_questv1;
        
        exprows = find(T.isPrac==0);
        exprows=exprows(2:end); % remove first target out of staircase.
        strows= find(T.isStationary==1);
        wkrows= find(T.isStationary==0);
        
        expContrasts= unique(T.targContrast(exprows));
        
        %note that there is a lag in the quest function. The first target
        %presented is outside the settled range of target contrasts.
        %so find the first targ contr, and set it's value to nearest in our
        %set.
        %single gc data:
        firstC_row= find(~isnan(Ppant_gaitData.targContrast),1,'first');
        changeC = Ppant_gaitData.targContrast(firstC_row);
        %find nearest as replacement.
        newC= dsearchn(expContrasts, changeC);
        Ppant_gaitData.targContrast(firstC_row) = expContrasts(newC);
        
        % same for doubGC data:
        firstC_row= find(~isnan(Ppant_gaitData_doubGC.targContrast),1,'first');
        changeC = Ppant_gaitData_doubGC.targContrast(firstC_row);
        %note that there can be extra data in the doubleGC case, so find
        %quickly:
        changeRows = find(Ppant_gaitData_doubGC.targContrast==changeC);
        %find nearest contrval as replacement.
        newC= dsearchn(expContrasts, changeC);
        Ppant_gaitData_doubGC.targContrast(changeRows) = expContrasts(newC);
        
        
        %intersect of experiment, and relevant condition:
        standingrows = intersect(strows, exprows);
        walkingrows = intersect(wkrows, exprows);
        
        % for entire condition (wlk. stand. without split by gait details),
        % extract the data (for accuracy and RT calcs).
        useRows=[];
        useRows{1} = standingrows;
        useRows{2} = walkingrows;
        
        contrastvalues = unique(T.targContrast(walkingrows));
        if length(contrastvalues)~=7
            error('check code');            
        end
        
        for iStWlk=1:2
            extractrows = useRows{iStWlk};
            
            % collect data for standing portion:

            trgContr = T.targContrast(extractrows);
            trgCorr= T.targCor(extractrows);
            
            
            OutOfNum = ones(1, size(trgContr,1));
            [~, NumPer, TotalPer] = PAL_MLDS_GroupTrialsbyX(trgContr, trgCorr,...
                OutOfNum);
            
            % also collect RTs per contr.
            trgRT = T.clickRT(extractrows);
            
            
            %convert contr to index (for binning).
            trgContrIDX = dsearchn(contrastvalues, trgContr);
            
            totalRTs=zeros(1,length(contrastvalues));
            
            for itargC=1:length(contrastvalues)
                tmpr=find(trgContrIDX==itargC);
                tmpRTs = trgRT(tmpr);
                % avoid negative (these were missed targs).
                tmpRTs = tmpRTs(tmpRTs>0);
                totalRTs(itargC) = nanmean(tmpRTs);
                
            end
            
          %store data:
            if iStWlk==1
                NumPerContr_standing = NumPer;
                TotalPerContr_standing = TotalPer;
                RTPerContr_standing = totalRTs;
            else
                NumPerContr_wlking = NumPer;
                TotalPerContr_wlking=TotalPer;
                RTPerContr_wlking= totalRTs;
            end
        end
        
        
        %% also precompute for different gait sizes (1-2), and L/R leading feet:
        
        for nGait=1:2
            if nGait==1
                pidx=pidx1; % indices for binning (single or double gc).
                ppantData= Ppant_gaitData;
                useL=Ltrials;
                useR=Rtrials;
                alltrials = 1:length(Ltrials);
            else
                pidx=pidx2;
                ppantData= Ppant_gaitData_doubGC;
                useL=Ltrials_db;
                useR=Rtrials_db;
                alltrials = 1:length(Ltrials_db);
                  
            end
            trialstoIndex ={useL, useR, alltrials}; 
            % split by gait/ foot (L/R)
            for iLR=1:3
                uset = trialstoIndex{iLR};
                
                %% %%%%%%%%%%%%%%%% 
                % Step through different data types :
                %%%%%%%%%%%%%%%%%% 
                
                  %% store the histcounts, for target contrast level per gaitsize, 
                  % and step (L/R).
                %Target onset data:
                targContrIDX = ppantData.targContrastIndx(uset);
                tPos= ppantData.targPosPcnt(uset); % gait pcnt for target onset.
                tCor = ppantData.targDetected(uset);
                
                targContr = ppantData.targContrast(uset);
                % restrict data to target presented (remove NaNs)                                
                rmnanContrst = ~isnan(targContrIDX);
                
                trgContr= targContr(rmnanContrst);
                trgCorr= tCor(rmnanContrst);
                trgPos = tPos(rmnanContrst);

                %Each entry of StimLevelsall corresponds to single trial
                OutOfNum = ones(1,size(trgContr,1));
                
                % pre compute result per subj
                [ValPerContr, NumPerContr_iLR, TotalPerContr_iLR] = PAL_MLDS_GroupTrialsbyX(trgContr, trgCorr,...
                OutOfNum);  
                %% Now also compute, but restrict to gait thirds.
               
                 [ValPerContr_gaitQntl, NumPerContr_gaitQntl, TotalPerContr_gaitQntl] = deal([]);
                
                 qntlBounds = round(quantile(0:pidx(end),nquants-1)); % 
                pcntBounds = [1, qntlBounds, pidx(end)];
                %n bounds,
                for iq=1:length(pcntBounds)-1
                    
                    tmpA = find(trgPos>pcntBounds(iq));
                    tmpB = find(trgPos<=pcntBounds(iq+1));
                    useC = intersect(tmpA,tmpB);
                    
                    trgContr_in = trgContr(useC);
                    trgCorr_in = trgCorr(useC);
                    OutOfNum= ones(1,size(trgContr_in,1));
                    
                    [ValPerContr_gaitQntl(iq,:),...
                        NumPerContr_gaitQntl(iq,:), ...
                        TotalPerContr_gaitQntl(iq,:)] = PAL_MLDS_GroupTrialsbyX(trgContr_in, trgCorr_in,...
                OutOfNum);  
                    
                end
                
                %% store
                
                if nGait==1
                    %using Targ pos as index:
                    %the counts per gait "%"
                    GFX_TargPosData(ippant,iLR).gc_ContrVals= ValPerContr;
                    %RTs:
                    GFX_TargPosData(ippant,iLR).gc_RTperContr_allwlking =RTPerContr_wlking;                    
                    GFX_TargPosData(ippant,iLR).gc_RTperContr_allstatnry=RTPerContr_standing;
                    %standing
                    GFX_TargPosData(ippant,iLR).gc_NumPerContr_allstatnry= NumPerContr_standing;                   
                    GFX_TargPosData(ippant,iLR).gc_TotalPerContr_allstatnry= TotalPerContr_standing;
                    %walking (all)
                    GFX_TargPosData(ippant,iLR).gc_NumPerContr_allwlking = NumPerContr_wlking;
                    GFX_TargPosData(ippant,iLR).gc_TotalPerContr_allwlking = TotalPerContr_wlking;
                    %walking (split by LR)
                    GFX_TargPosData(ippant,iLR).gc_NumPerContr_LRwlking = NumPerContr_iLR;
                    GFX_TargPosData(ippant,iLR).gc_TotalPerContr_LRwlking = TotalPerContr_iLR;
                    %walking (split by LR, and  % gait cycle)
                    GFX_TargPosData(ippant,iLR).gc_NumPerContr_qntlwlking = NumPerContr_gaitQntl;
                    GFX_TargPosData(ippant,iLR).gc_TotalPerContr_qntlwlking = TotalPerContr_gaitQntl;
                    GFX_TargPosData(ippant,iLR).gc_qntl_bounds= pcntBounds;
                    
                else
                   %using Targ pos as index:
                    %the counts per gait "%"
                     GFX_TargPosData(ippant,iLR).doubgc_ContrVals= ValPerContr;
                     %RTs:
                    GFX_TargPosData(ippant,iLR).doubgc_RTperContr_allwlking =RTPerContr_wlking;                    
                    GFX_TargPosData(ippant,iLR).doubgc_RTperContr_allstatnry=RTPerContr_standing;
                   
                    %standing
                    GFX_TargPosData(ippant,iLR).doubgc_NumPerContr_allstatnry= NumPerContr_standing;                   
                    GFX_TargPosData(ippant,iLR).doubgc_TotalPerContr_allstatnry= TotalPerContr_standing;
                    %walking (all)
                    GFX_TargPosData(ippant,iLR).doubgc_NumPerContr_allwlking = NumPerContr_wlking;
                    GFX_TargPosData(ippant,iLR).doubgc_TotalPerContr_allwlking = TotalPerContr_wlking;
                    %walking (split by LR)
                    GFX_TargPosData(ippant,iLR).doubgc_NumPerContr_LRwlking = NumPerContr_iLR;
                    GFX_TargPosData(ippant,iLR).doubgc_TotalPerContr_LRwlking = TotalPerContr_iLR;
                    %walking (split by LR, and  % gait cycle)
                    GFX_TargPosData(ippant,iLR).doubgc_NumPerContr_qntlwlking = NumPerContr_gaitQntl;
                    GFX_TargPosData(ippant,iLR).doubgc_TotalPerContr_qntlwlking = TotalPerContr_gaitQntl;
                    GFX_TargPosData(ippant,iLR).doubgc_qntl_bounds= pcntBounds;
                end
                
            end % iLR
            
        end % nGait
        
        
    end % ppant
    
    
        cd([datadir filesep 'ProcessedData' filesep 'GFX']);
        
        
    save('GFX_Data_inGaits_PFfits', ...
        'GFX_headY', 'GFX_TargPosData',...
       'subjIDs');%, '-append');
else
    cd([datadir filesep 'ProcessedData' filesep 'GFX']);
    load('GFX_Data_inGaits_PFfits');
end
%% now for plotting jobs
% 
%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%


%% plots at Participant level:

%%
%PFX
if job.plot_PsychmFits_pfx% target onset relative to gait.
 %% for each participant, plot the PM fits for standing and walking
 % compares L and R ft also.
 cfg=[];
 cfg.subjIDs = subjIDs;
 cfg.type = 'Target';
 cfg.datadir= datadir; % for orienting to figures folder
 cfg.HeadData= GFX_headY;
 cfg.pidx1= pidx1;
 cfg.pidx2= pidx2;
 cfg.plotlevel='PFX';
 % cycles through ppants, plots with correct labels.
 plot_PFfits_comparison(GFX_TargPosData, cfg);
 
end

%%
if job.plot_PsychmFits_gait_pfx
    %% 
    cfg=[];
 cfg.subjIDs = subjIDs;
 cfg.type = 'Target';
 cfg.datadir= datadir; % for orienting to figures folder
 cfg.HeadData= GFX_headY;
 cfg.pidx1= pidx1;
 cfg.pidx2= pidx2;
 cfg.plotlevel='PFX';
 % cycles through ppants, plots with correct labels.
 plot_PFfits_gaitcycle(GFX_TargPosData, cfg);
 
end
%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
%% GFX : group effects plots
%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
if job.plot_PsychmFits_GFX% target onset relative to gait.
 %% for each participant, plot the PM fits for standing and walking
 % compares L and R ft also.
 cfg=[];
 cfg.subjIDs = subjIDs;
 cfg.type = 'Target';
 cfg.datadir= datadir; % for orienting to figures folder
 cfg.HeadData= GFX_headY;
 cfg.pidx1= pidx1;
 cfg.pidx2= pidx2;
 cfg.plotlevel='GFX';
 % cycles through ppants, plots with correct labels.
 plot_PFfits_comparison(GFX_TargPosData, cfg);
 
end

%%

if job.plot_PsychmFits_gait_GFX
    %% 
    cfg=[];
 cfg.subjIDs = subjIDs;
 cfg.type = 'Target';
 cfg.datadir= datadir; % for orienting to figures folder
 cfg.HeadData= GFX_headY;
 cfg.pidx1= pidx1;
 cfg.pidx2= pidx2;
 cfg.plotlevel='GFX';
 % cycles through ppants, plots with correct labels.
 plot_PFfits_gaitcycle(GFX_TargPosData, cfg);
 
end
%%